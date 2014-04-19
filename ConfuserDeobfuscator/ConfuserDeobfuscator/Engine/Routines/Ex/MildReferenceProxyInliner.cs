using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines.Ex
{
    class MildReferenceProxyInliner : DeobfuscationRoutineEx
    {
        struct ReferenceProxy
        {
            public MethodDef SourceMethod { get; set; }
            public MethodDef TargetMethod { get; set; }
            public Instruction CallerInstruction { get; set; }

            public ReferenceProxy(MethodDef sourceMethod, MethodDef targetMethod, Instruction callerInstruction)
                : this()
            {
                SourceMethod = sourceMethod;
                TargetMethod = targetMethod;
                CallerInstruction = callerInstruction;
            }
        }

        public override string Title
        {
            get { return "Resolving reference proxies"; }
        }

        public override bool Detect()
        {
            var proxies = new List<ReferenceProxy>();

            foreach (var method in Ctx.Assembly.FindMethods(m => m.HasBody))
            {
                if (method.IsConstructor && method.Parameters.Count == 2 && method.DeclaringType.Name.ToString().ToLower().StartsWith("attribute0"))
                {
                    method.Body.Instructions.Replace(
                        method.Body.Instructions.First(x => x.OpCode.Code == Code.Ldarg_1),
                        new Instruction(OpCodes.Ldc_I4, 0x521ee802));
                    method.Body.UpdateInstructionOffsets();
                    CalculateMutations(method);
                }

                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode.Code != Code.Call && instr.OpCode.Code != Code.Callvirt &&
                        instr.OpCode.Code != Code.Newobj) continue;

                    if (!(instr.Operand is MethodDef))
                        continue;

                    var target = instr.Operand as MethodDef;

                    if (!CanInline(target))
                        continue;

                    proxies.Add(new ReferenceProxy(method, target, instr));
                }
            }

            RoutineVariables.Add("proxies", proxies);

            return true;
        }

        public override void Initialize()
        {
            
        }

        public override void Process()
        {
            var proxies = RoutineVariables["proxies"] as List<ReferenceProxy>;

            if (proxies == null) return;

            foreach (var proxy in proxies)
            {
                proxy.SourceMethod.Body.Instructions.Replace(proxy.CallerInstruction,
                    proxy.TargetMethod.Body.Instructions.First(x => x.IsCall() || x.OpCode.Code == Code.Newobj));
            }
        }

        public override void CleanUp()
        {
            var proxies = RoutineVariables["proxies"] as List<ReferenceProxy>;

            foreach (var proxy in proxies)
            {
                // Some methods have a null declaringtype ???
                if (proxy.TargetMethod.DeclaringType == null)
                    continue;
                proxy.TargetMethod.DeclaringType.Methods.Remove(proxy.TargetMethod);
            }
        }

        // Detect confuser added methods..
        public bool CanInline(MethodDef method)
        {
            if (!method.IsStatic)
                return false;

            if (!method.IsPrivateScope)
                return false;

            if (!method.HasBody)
                return false;

            if (!method.Body.Instructions.Last().Previous(method.Body).IsCall() &&
                method.Body.Instructions.Last().Previous(method.Body).OpCode.Code != Code.Newobj)
                return false;

            for (var i = 0; i < method.Parameters.Count; i++)
                if (!method.Body.Instructions[i].IsLdarg())
                    return false;

            if (!method.Body.Instructions[method.Parameters.Count].IsCall() &&
                method.Body.Instructions[method.ParamDefs.Count].OpCode.Code != Code.Newobj)
                return false;

            return true;
        }
    }
}
