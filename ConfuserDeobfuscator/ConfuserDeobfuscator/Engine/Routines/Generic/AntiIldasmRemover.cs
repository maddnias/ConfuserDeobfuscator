using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;


namespace ConfuserDeobfuscator.Engine.Routines.Generic
{
    class AntiIldasmRemover : DeobfuscationRoutine19R
    {
        public override string Title
        {
            get { return "Removing anti-ildasm attribute"; }
        }

        public override bool Detect()
        {
            var mod = Ctx.Assembly.ManifestModule;

            RoutineVariables.Add("mod", mod);
            foreach (var attrib in mod.CustomAttributes)
                if (attrib.AttributeType.Name == "SuppressIldasmAttribute")
                {
                    RoutineVariables.Add("badAttribute", attrib);
                    break;
                }

            if (RoutineVariables.Count == 1)
            {
                Ctx.UIProvider.WriteVerbose("No anti-ildasm?");
                return false;
            }

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
            var mod = RoutineVariables["mod"] as ModuleDef;
            var badAttrib = RoutineVariables["badAttribute"] as CustomAttribute;

            if (mod != null && badAttrib != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed anti-ildasm attribute from manifest module", 2, true, "");
                mod.CustomAttributes.Remove(badAttrib);
            }
        }
    }
}
