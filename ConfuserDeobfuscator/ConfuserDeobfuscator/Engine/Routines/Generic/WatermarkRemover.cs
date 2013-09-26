using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Routines.Generic
{
    public class WatermarkRemover : DeobfuscationRoutine19R
    {
        public override string Title
        {
            get { return "Removing confuser watermark"; }
        }

        public override bool Detect()
        {
            var mod = DeobfuscatorContext.Assembly.Modules[0];

            if (!mod.HasCustomAttributes)
            {
                DeobfuscatorContext.UIProvider.Write("No watermark?");
                return false;
            }

            foreach (var attrib in mod.CustomAttributes.Where(
                attrib => attrib.TypeFullName.EndsWith("ConfusedByAttribute")))
            {
                RoutineVariables.Add("module", mod);
                RoutineVariables.Add("badattribute", attrib);
                break;
            }

            if (RoutineVariables.Count != 2)
            {
                DeobfuscatorContext.UIProvider.Write("No watermark?");
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
            var attrib = RoutineVariables["badattribute"] as CustomAttribute;
            var mod = RoutineVariables["module"] as ModuleDef;

            if (mod == null) return;
            mod.CustomAttributes.Remove(attrib);
            DeobfuscatorContext.UIProvider.WriteVerbose("Removed ConfusedByAttribute from {0}", 2, true, mod.Name);

            if (attrib == null) return;
            attrib.AttributeType.Module.Types.Remove(attrib.AttributeType.ResolveTypeDef());
            DeobfuscatorContext.UIProvider.WriteVerbose("Removed type {0}", 2, true, attrib.AttributeType.Name.String);
        }
    }
}
