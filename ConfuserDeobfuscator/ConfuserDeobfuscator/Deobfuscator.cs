using System;
using System.Collections.Generic;
using System.IO;
using ConfuserDeobfuscator.Engine;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Generic;
using ConfuserDeobfuscator.Engine.Routines._1._9;
using dnlib.DotNet;

namespace ConfuserDeobfuscator
{
    public class ConsoleProvider : IUserInterfaceProvider
    {
        public void WriteVerbose(string data, int node)
        {
            for (var i = 0; i < node; i++)
                data = data.Insert(0, "-");
            if(node > 0)
                data = data.Insert(node, " ");

            Console.WriteLine(data);
        }

        public void WriteVerbose(string formattedData, int node, params object[] data)
        {
            for (var i = 0; i < node; i++)
                formattedData = formattedData.Insert(0, "-");
            if (node > 0)
            formattedData = formattedData.Insert(node, " ");

            Console.WriteLine(formattedData, data);
        }

        public void Write(string data, int node)
        {
            for (var i = 0; i < node; i++)
                data = data.Insert(0, "-");
            if (node > 0)
            data = data.Insert(node, " ");

            Console.WriteLine(data);
        }

        public void Write(string formattedData, int node, params object[] data)
        {
            for (var i = 0; i < node; i++)
                formattedData = formattedData.Insert(0, "-");
            if (node > 0)
            formattedData = formattedData.Insert(node, " ");

            Console.WriteLine(formattedData, data);
        }
    }

    public class Deobfuscator
    {
        private readonly Dictionary<string, Pipeline> _deobfuscationPipelines = new Dictionary<string, Pipeline>
                                                                           {
                                                                               {"unpacking", new Pipeline()},
                                                                               {"preRoutines", new Pipeline()},
                                                                               {"routines", new Pipeline()}
                                                                           };

        private void InitializePipelines()
        {
            var unpackingPipeline = _deobfuscationPipelines["unpacking"];
           // unpackingPipeline.AppendStep(new MethodCleaner(true));
           // unpackingPipeline.AppendStep(new Unpacker());

            var preRoutinePipeline = _deobfuscationPipelines["preRoutines"];
            preRoutinePipeline.AppendStep(new InvalidMetadataRemover());
            preRoutinePipeline.AppendStep(new MethodCleaner(false));
            preRoutinePipeline.AppendStep(new MethodDecryptor());

            var routinePipeline = _deobfuscationPipelines["routines"];
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

        public void ProcessFile(string filename)
        {
            DeobfuscatorContext.Filename = filename;
            DeobfuscatorContext.Assembly = AssemblyDef.Load(filename);
            DeobfuscatorContext.UIProvider = new ConsoleProvider();
            DeobfuscatorContext.LoggingLevel = DeobfuscatorContext.OutputLevel.Verbose;
            DeobfuscatorContext.OriginalMD = (DeobfuscatorContext.Assembly.ManifestModule as ModuleDefMD);

            DeobfuscatorContext.UIProvider.Write("Initializing deobfuscation of {0}...",
                                                 0, Path.GetFileNameWithoutExtension(filename));

            InitializePipelines();
            _deobfuscationPipelines["unpacking"].Process();
            _deobfuscationPipelines["preRoutines"].Process();
            _deobfuscationPipelines["routines"].Process();


            DeobfuscatorContext.Assembly.Write(Path.GetDirectoryName(filename) + "\\" +
                                               Path.GetFileNameWithoutExtension(filename) + "_cleaned" +
                                               Path.GetExtension(filename));
        }
    }
}
