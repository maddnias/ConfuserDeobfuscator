using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Deobfuscators.Base;
using ConfuserDeobfuscator.Engine;
using ConfuserDeobfuscator.Engine.Routines.Ex;
using ConfuserDeobfuscator.Engine.Routines.Generic;
using ConfuserDeobfuscator.Engine.Routines._1._8;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Deobfuscators
{
    public class DeobfuscatorEx : DeobfuscatorBase
    {
        public override Dictionary<string, Pipeline> Pipelines { get; set; }

        public override void InitializePipelines()
        {
            Pipelines = new Dictionary<string, Pipeline>
            {
                {"routines", new Pipeline()}
            };

            Pipelines["routines"].AppendStep(new MildReferenceProxyInliner());
            Pipelines["routines"].AppendStep(new MethodCleaner(false));
            Pipelines["routines"].AppendStep(new NativeSwitchEvaluator());
            //Pipelines["routines"].AppendStep(new MethodCleaner(false));
        }

        public override void ProcessFile(string filename)
        {
            Ctx.Deobfuscator = this;
            DeobfuscateAssembly(filename);
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
            Pipelines["routines"].Process();

            FinalizeDeobfuscation();
        }
    }
}
