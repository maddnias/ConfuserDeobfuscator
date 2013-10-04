using System;
using System.Collections.Generic;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine
{
    public class AntiDebugRemover : DeobfuscationRoutine19R
    {
        public override string Title { get { return "Removing anti-debug module"; } }


        public override void Initialize()
        {
        }

        public override bool Detect()
        {
            var antiDebug = Ctx.Assembly.FindMethod(IsAntiDebugMethod);
            var cctor = Ctx.Assembly.Modules[0].Types.FirstOrDefault(t => t.Name == "<Module>").GetStaticConstructor();

            if (antiDebug == null || cctor == null)
            {
                Ctx.UIProvider.WriteVerbose("No anti-debug protection?");
                return false;
            }

            var refs = antiDebug.FindAllReferences().ToList();

            if (refs.Count != 1)
            {
                Ctx.UIProvider.WriteVerbose("Too many or too few calls to anti-debug module");
                return false;
            }

            RoutineVariables.Add("refs", refs);
            RoutineVariables.Add("antiDebug", antiDebug);
            RoutineVariables.Add("cctor", cctor);

            return true;
        }

        public override void Process()
        {
            var antiDebug = RoutineVariables["antiDebug"] as MethodDef;
            var refs = RoutineVariables["refs"] as List<Tuple<Instruction, MethodDef>>;

            RoutineVariables.Add("badcall", refs[0]);
            RoutineVariables.Add("badtype", antiDebug.DeclaringType);
        }

        public override void CleanUp()
        {
            var badCall = RoutineVariables["badcall"] as Tuple<Instruction, MethodDef>;
            var badType = RoutineVariables["badtype"] as TypeDef;

            if (badCall != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed bad call from {0}::{1}", 2, true, badCall.Item2.DeclaringType.Name,
                                            badCall.Item2.Name);
                RemovedInstructions.Add(Tuple.Create(badCall.Item2, new[] {badCall.Item1}));
                badCall.Item2.Body.Instructions.Remove(badCall.Item1);
            }

            if (badType != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed bad type {0}", 2, true, badType.Name);
                badType.Module.Types.Remove(badType);
            }
        }

        private bool IsAntiDebugMethod(MethodDef mDef)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.Parameters.Count != 0 || !mDef.Body.HasVariables)
                return false;

            if (!(mDef.Body.Variables.Count != 1 || mDef.Body.Variables.Count != 2))
                return false;

            if (mDef.Body.Instructions.FindInstruction(x => x.IsCall() && x.Operand.ToString().Contains("GetEnvironmentVariable"), 0) == null
                || mDef.Body.Instructions.FindInstruction(x => x.IsCall() && x.Operand.ToString().Contains("GetEnvironmentVariable"), 1) == null)
                return false;

            if (mDef.DeclaringType == null || mDef.DeclaringType.Methods.Count != 7)
                return false;

            return true;
        }
    }
}
