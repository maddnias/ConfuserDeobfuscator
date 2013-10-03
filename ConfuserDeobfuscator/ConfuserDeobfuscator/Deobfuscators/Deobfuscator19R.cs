using System.Collections.Generic;
using System.IO;
using ConfuserDeobfuscator.Deobfuscators.Base;
using ConfuserDeobfuscator.Engine;
using ConfuserDeobfuscator.Engine.Routines.Generic;
using ConfuserDeobfuscator.Engine.Routines._1._9;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Deobfuscators
{
    public class Deobfuscator19R : DeobfuscatorBase
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
            unpackingPipeline.AppendStep(new Unpacker());

            var preRoutinePipeline = Pipelines["preRoutines"];
            preRoutinePipeline.AppendStep(new InvalidMetadataRemover());
            preRoutinePipeline.AppendStep(new MethodCleaner(false));
            preRoutinePipeline.AppendStep(new MethodDecryptor());

            var routinePipeline = Pipelines["routines"];
            routinePipeline.AppendStep(new MethodCleaner(true));
            routinePipeline.AppendStep(new AntiTamperRemover());
            routinePipeline.AppendStep(new InvalidMetadataRemover());
            routinePipeline.AppendStep(new MtdProxyRemover());
            routinePipeline.AppendStep(new CtorProxyRemover());
            routinePipeline.AppendStep(new AntiDebugRemover());
            routinePipeline.AppendStep(new AntiDumpRemover());
            routinePipeline.AppendStep(new ResourceDecryptor());
            routinePipeline.AppendStep(new ConstantsDecryption());
            routinePipeline.AppendStep(new AntiIldasmRemover());
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
            var tester = new Unpacker();
            tester.Initialize();
            if (tester.Detect())
            {
               // Ctx.Assembly = DeobfuscateAssembly(Ctx.Assembly);
              //  Pipelines["unpacking"].Process();
            }
            Ctx.Assembly = DeobfuscateAssembly(Ctx.Assembly);
            FinalizeDeobfuscation();
        }
    }
}
