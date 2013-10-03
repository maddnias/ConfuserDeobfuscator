using System.Collections.Generic;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    class InvalidMetadataRemover : DeobfuscationRoutine19R, IMetadataWorker
    {
        public ModuleDefMD ModMD { get; set; }

        public override string Title
        {
            get { return "Removing invalid metadata"; }
        }

        public override bool Detect()
        {
            var initTypeName = ModMD.StringsStream.Read(ModMD.TablesStream.ReadTypeDefRow(1).Name);
            if (initTypeName.String.Length < 20 && initTypeName == "<Module>")
            {
                Ctx.UIProvider.WriteVerbose("No invalid metadata?");
                return false;
            }

            return true;
        }

        public override void Initialize()
        {
            ModMD = (Ctx.OriginalMD);
        }

        public override void Process()
        {
            var newName = ModMD.StringsStream.Read(ModMD.TablesStream.ReadTypeDefRow(1).Name); // Needs to be <Module> for deobfuscation
            // Should always be ModuleDef.Types[0], but i'll use name to be sure
            var mainType = Ctx.Assembly.ManifestModule.Types.FirstOrDefault(x => x.Name == newName);
            if (mainType == null)
                mainType = Ctx.Assembly.ManifestModule.Types[0];
            RoutineVariables.Add("<module>", mainType);

            var junkResources = new List<EmbeddedResource>();
            foreach (var res in Ctx.Assembly.ManifestModule.Resources)
                if (res is EmbeddedResource && res.Name.String.Length >= 500 && res.IsPrivate)
                    if ((res as EmbeddedResource).GetResourceData().Length == 0)
                        junkResources.Add(res as EmbeddedResource);
            RoutineVariables.Add("junkRes", junkResources);
        }

        public override void CleanUp()
        {
            var mainType = RoutineVariables["<module>"] as TypeDef;
            var junkResources = RoutineVariables["junkRes"] as List<EmbeddedResource>;

            if (mainType != null)
            {
                Ctx.UIProvider.WriteVerbose("Updated type at RID 1 name to <Module>", 2, true, "");
                Ctx.Assembly.ManifestModule.Types.First(x => x == mainType).Name = "<Module>";
            }

            if (junkResources != null)
            {
                junkResources.ForEach(r =>
                                          {
                                              Ctx.UIProvider.WriteVerbose("Removed junk resource");
                                              Ctx.Assembly.ManifestModule.Resources.Remove(r);
                                          });
            }
        }
    }
}
