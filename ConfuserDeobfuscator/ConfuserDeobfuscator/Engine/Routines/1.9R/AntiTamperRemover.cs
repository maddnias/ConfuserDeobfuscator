using System;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    public class AntiTamperRemover : DeobfuscationRoutine19R
    {
        public override string Title
        {
            get { return "Removing anti-tamper module"; }
        }

        public override bool Detect()
        {
            var antiTamper = Ctx.Assembly.FindMethod(IsAntiTamper);
            var cctor = Ctx.Assembly.GetRootType().GetStaticConstructor();

            RoutineVariables.Add("cctor", cctor);
            RoutineVariables.Add("antiTamper", antiTamper);
            var decryptor = Ctx.Assembly.FindMethod(IsDecryptor);

            if (antiTamper == null || decryptor == null)
                return false;

            RoutineVariables.Add("decryptor", decryptor);
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
            var antiTamper = RoutineVariables["antiTamper"] as MethodDef;
            var refs = antiTamper.FindAllReferences().ToList();

            if (refs.Count != 1)
            {
                Ctx.UIProvider.Write("Too many or too few references to anti-tamper module");
                return;
            }

            Ctx.UIProvider.WriteVerbose("Removed call to anti-tamper from {0}::{1}", 2, true, refs[0].Item2.DeclaringType.Name,
                                        ".cctor");

            RemovedInstructions.Add(Tuple.Create(refs[0].Item2, new[] {refs[0].Item1}));
            refs[0].Item2.Body.Instructions.Remove(refs[0].Item1);

            Ctx.UIProvider.WriteVerbose("Removed bad type: {0}", 2, true, antiTamper.DeclaringType.Name);
            Ctx.Assembly.ManifestModule.Types.Remove(antiTamper.DeclaringType);
        }

        public bool IsDecryptor(MethodDef mDef)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.ParamDefs.Count != 3 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Body.Variables.Count != 9 && mDef.Body.Variables.Count != 10)
                return false;

            var refs = mDef.FindAllReferences();

            if (refs.Count() != 1)
                return false;

            var flag = false;

            foreach (var instr in (RoutineVariables["cctor"] as MethodDef).Body.Instructions.Where(x => x.IsCall()))
            {
                if ((instr.Operand as MethodDef) == refs.ToList()[0].Item2)
                {
                    flag = true;
                    break;
                }
            }

            return flag;

           // if (mDef.FindAllReferences().ToList()[0].Item2 != RoutineVariables["antiTamper"])
           //     return false;
        }

        public static bool IsAntiTamper(MethodDef mDef)
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

            return true;
        }
    }
}
