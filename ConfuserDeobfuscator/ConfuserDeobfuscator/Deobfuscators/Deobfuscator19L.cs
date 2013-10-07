using System.Collections.Generic;
using System.IO;
using ConfuserDeobfuscator.Deobfuscators.Base;
using ConfuserDeobfuscator.Engine;
using ConfuserDeobfuscator.Engine.Routines.Generic;
using ConfuserDeobfuscator.Engine.Routines;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Deobfuscators
{
    public class Deobfuscator19L : DeobfuscatorBase
    {
        public override Dictionary<string, Pipeline> Pipelines { get; set; }

        public override void InitializePipelines()
        {
            Pipelines = new Dictionary<string, Pipeline>
            {
                {"unpacking", new Pipeline()},
                {"preRoutines", new Pipeline()},
                {"routines", new Pipeline()}
            };

            var unpackingPipeline = Pipelines["unpacking"];
            unpackingPipeline.AppendStep(new Engine.Routines._1._9.Unpacker());

            var preRoutinePipeline = Pipelines["preRoutines"];
            preRoutinePipeline.AppendStep(new Engine.Routines._1._9.InvalidMetadataRemover());
            preRoutinePipeline.AppendStep(new MethodCleaner(false));
            preRoutinePipeline.AppendStep(new Engine.Routines._1._9.MethodDecryptor());

            var routinePipeline = Pipelines["routines"];
            routinePipeline.AppendStep(new MethodCleaner(true));
            routinePipeline.AppendStep(new Engine.Routines._1._9.InvalidMetadataRemover());
            routinePipeline.AppendStep(new Engine.Routines._1._9.MtdProxyRemover());
            routinePipeline.AppendStep(new Engine.Routines._1._9.CtorProxyRemover());
            routinePipeline.AppendStep(new AntiDebugRemover());
            routinePipeline.AppendStep(new Engine.Routines._1._9.AntiDumpRemover());
            routinePipeline.AppendStep(new Engine.Routines._1._9L.ResourceDecryptor());
            routinePipeline.AppendStep(new Engine.Routines._1._9L.ConstantsDecryption());
            routinePipeline.AppendStep(new AntiIldasmRemover());
            routinePipeline.AppendStep(new Engine.Routines._1._9.AntiTamperRemover());
            routinePipeline.AppendStep(new WatermarkRemover());
        }

        public override void ProcessFile(string filename)
        {
            Ctx.Deobfuscator = this;
            DeobfuscateAssembly(filename);
        }

        public override AssemblyDef DeobfuscateAssembly(AssemblyDef asmDef)
        {
            Ctx.Assembly = asmDef;
            Ctx.OriginalMD = (Ctx.Assembly.ManifestModule as ModuleDefMD);

            InitializePipelines();
            Pipelines["preRoutines"].Process();
            Pipelines["routines"].Process();

            return Ctx.Assembly;
        }

        public override void DeobfuscateAssembly(string filename)
        {
            Ctx.Filename = filename;
            Ctx.Assembly = AssemblyDef.Load(filename);
            Ctx.UIProvider = new ConsoleProvider();
            Ctx.LoggingLevel = Ctx.OutputLevel.Normal;
            Ctx.OriginalMD = (Ctx.Assembly.ManifestModule as ModuleDefMD);

            Ctx.UIProvider.Write("Initializing deobfuscation of {0}\n_______________________________________\n",
                                                 0, true, Path.GetFileNameWithoutExtension(filename));

            InitializePipelines();
            Ctx.Assembly = DeobfuscateAssembly(Ctx.Assembly);
            FinalizeDeobfuscation();
        }
    }
}
