using System;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    class AntiDumpRemover : DeobfuscationRoutine
    {
        public override string Title
        {
            get { return "Removing anti-dump module..."; }
        }

        public override bool Detect()
        {
            var antiDump = Ctx.Assembly.FindMethod(IsAntiDumpMethod);

            if (antiDump == null)
            {
                Ctx.UIProvider.Write("No anti-dump?");
                return false;
            }
            RoutineVariables.Add("antidump", antiDump);
            var refs = antiDump.FindAllReferences().ToList();

            if (refs.Count != 1)
            {
                Ctx.UIProvider.Write("Too many or too few calls to anti-debug module");
                return false;
            }
            RoutineVariables.Add("badcall", refs[0]);

            return true;
        }

        public override void Initialize()
        {

        }

        public override void Process()
        {

        }

        public override void CleanUp()
        {
            var antiDump = RoutineVariables["antidump"] as MethodDef;
            var badCall = RoutineVariables["badcall"] as Tuple<Instruction, MethodDef>;

            if (antiDump != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed bad type {0}", 2, antiDump.DeclaringType.Name);
                Ctx.Assembly.ManifestModule.Types.Remove(antiDump.DeclaringType);
            }

            if (badCall != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed call to anti-dump module", 2, "");
                badCall.Item2.Body.Instructions.Remove(badCall.Item1);
            }
        }

        private bool IsAntiDumpMethod(MethodDef mDef)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.ParamDefs.Count != 0 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Body.Variables.Count != 43 && mDef.Body.Variables.Count != 44)
                return false;

            if (
                mDef.Body.Instructions.FindInstruction(
                    i => i.IsCall() && i.Operand.ToString().Contains("GetHINSTANCE"), 0) == null)
                return false;
            if (mDef.Body.Instructions.Count <= 900)
                return false;

            return true;
        }
    }
}
