using System;
using System.Collections.Generic;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    public class MtdProxyRemover : DeobfuscationRoutine19R, IMetadataWorker
    {
        public ModuleDefMD ModMD { get; set; }

        public override string Title
        {
            get { return "Resolving method proxies"; }
        }

        public override bool Detect()
        {
            RoutineVariables.Add("native", false); // I'll have to look into the native proxies later
            var proxies = new List<TypeDef>();
           

            foreach (var del in Ctx.Assembly.ManifestModule.Types.Where(t => t.BaseType != null))
            {
                if (!del.BaseType.FullName.Contains("System.MulticastDelegate"))
                    continue;
                if (del.GetStaticConstructor() == null)
                    continue;
                if (!del.GetStaticConstructor().HasBody)
                    continue;
                // We have to distinguish method proxy generator from constructor proxy generator
                var generator = del.GetStaticConstructor().Body.Instructions.FindInstruction(x => x.IsCall(), 0).Operand as MethodDef;
                if(generator.Body.Instructions.FindInstruction(x => x.OpCode.Code == Code.Isinst && x.Operand.ToString().Contains("MethodInfo"), 0) != null)
                    proxies.Add(del);
            }

            if (proxies.Count == 0)
            {
                Ctx.UIProvider.WriteVerbose("No method proxies?");
                return false;
            }
            RoutineVariables.Add("proxies", proxies);
            return true;
        }

        public override void Initialize()
        {
            ModMD = Ctx.Assembly.ManifestModule as ModuleDefMD;
        }

        public override void Process()
        {
            var proxies = RoutineVariables["proxies"] as List<TypeDef>;
            var resolvedProxies = new List<Tuple<TypeDef, List<Tuple<IMemberRef, FieldDef>>>>();

            if (proxies == null) return;
            var tester = proxies[0];
            var proxyGenerator =
                tester.GetStaticConstructor().Body.Instructions.FindInstruction(x => x.IsCall(), 0).Operand as MethodDef;
            CalculateMutations(proxyGenerator);
            var key = ReadKey(proxyGenerator);
            var virtIdentifier =
                proxyGenerator.Body.Instructions.FindInstruction(
                    x => x.OpCode.Code == Code.Callvirt && x.Operand.ToString().EndsWith("get_Chars(System.Int32)"), 0)
                              .Next(proxyGenerator.Body)
                              .GetLdcI4Value();

            proxies.ForEach(p =>
                                {
                                    var mDefs = ResolveProxyMethod(p, key).ToList();
                                    if (mDefs.Count > 0)
                                        resolvedProxies.Add(Tuple.Create(p, mDefs));
                                });
            resolvedProxies.ForEach(p =>
                                        {
                                            foreach (var call in p.Item2)
                                                RestoreProxyCall(Tuple.Create(p.Item1, call.Item1, call.Item2), (char)virtIdentifier);
                                        });
            RoutineVariables.Add("generator", proxyGenerator);
        }

        public override void CleanUp()
        {
            var proxies = RoutineVariables["proxies"] as List<TypeDef>;
            var generator = RoutineVariables["generator"] as MethodDef;

            if (proxies != null)
                foreach (var proxy in proxies)
                {
                    Ctx.UIProvider.WriteVerbose("Removed method proxy: {0}", 2, true, proxy.Name);
                    Ctx.Assembly.ManifestModule.Types.Remove(proxy);
                }

            if (generator != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed method proxy generator {0}::{1}", 2, true, generator.DeclaringType.Name,
                                            generator.Name);
                generator.DeclaringType.Methods.Remove(generator);
            }
        }

        public override void RestoreProxyCall(Tuple<TypeDef, IMemberRef, FieldDef> resolvedProxy, char virtIdentifier)
        {
            var destCall =
                resolvedProxy.Item1.Methods.First(
                    x =>
                    x.IsStatic && !x.IsConstructor && x.HasBody &&
                    x.Body.Instructions.FindInstruction(
                        y => y.OpCode.Code == Code.Ldsfld && y.Operand == resolvedProxy.Item3, 0) != null);

            var isVirtual = resolvedProxy.Item3.Name.String[0] == virtIdentifier;

            foreach (var @ref in destCall.FindAllReferences())
            {
                if (resolvedProxy.Item2 == null)
                    continue;
                Ctx.UIProvider.WriteVerbose("Restored proxy call [{0} -> {1}]", 2, true, destCall.Name, resolvedProxy.Item2.Name);
                @ref.Item2.Body.SimplifyMacros(@ref.Item2.Parameters);
                @ref.Item2.Body.SimplifyBranches();
                if ((resolvedProxy.Item2 as MemberRef) == null)
                    return;

                RemovedInstructions.Add(Tuple.Create(@ref.Item2, new[]
                {
                    @ref.Item1
                }));
                @ref.Item2.Body.Instructions.Replace(@ref.Item1,
                      Instruction.Create((isVirtual ? OpCodes.Callvirt : OpCodes.Call), resolvedProxy.Item2 as MemberRef));

                @ref.Item2.Body.OptimizeMacros();
                @ref.Item2.Body.OptimizeBranches();
                @ref.Item2.Body.UpdateInstructionOffsets();
            }
        }
    }
}
