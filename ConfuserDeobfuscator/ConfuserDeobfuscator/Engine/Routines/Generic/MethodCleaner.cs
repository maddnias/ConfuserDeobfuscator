using System.Collections.Generic;
using System.IO;
using ConfuserDeobfuscator.Engine.Base;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.cui;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace ConfuserDeobfuscator.Engine.Routines.Generic
{
    class MethodCleaner : DeobfuscationRoutine
    {
        public override string Title
        {
            get { return "Cleaning methods via de4dot..."; }
        }

        public bool Rename { get; set; }

        static readonly IList<IDeobfuscatorInfo> deobfuscatorInfos = createDeobfuscatorInfos();

        private static IList<IDeobfuscatorInfo> createDeobfuscatorInfos()
        {
            return new List<IDeobfuscatorInfo>
                       {
                           new de4dot.code.deobfuscators.Unknown.DeobfuscatorInfo(),
                       };
        }

        public MethodCleaner(bool rename)
        {
            Rename = rename;
        }

        public override bool Detect()
        {
            return true;
        }

        public override void Initialize()
        {
    
        }

        public override void Process()
        {
            IList<IObfuscatedFile> files = new List<IObfuscatedFile>();
            var filesOptions = new FilesDeobfuscator.Options();
            filesOptions.DeobfuscatorInfos = deobfuscatorInfos;
            filesOptions.AssemblyClientFactory = new NewAppDomainAssemblyClientFactory();
            filesOptions.RenameSymbols = true;
            filesOptions.ControlFlowDeobfuscation = true;
            filesOptions.KeepObfuscatorTypes = false;
            filesOptions.MetaDataFlags = MetaDataFlags.PreserveAll;
            filesOptions.Files = files;

            var newFileOptions = new ObfuscatedFile.Options
            {
                Filename = DeobfuscatorContext.Filename,
                NewFilename = "XPADDING", 
                ControlFlowDeobfuscation = filesOptions.ControlFlowDeobfuscation,
                RenamerFlags = filesOptions.RenamerFlags,
                KeepObfuscatorTypes = filesOptions.KeepObfuscatorTypes,
                MetaDataFlags = filesOptions.MetaDataFlags,
                PreserveTokens = true
            };

            using (var asm = new MemoryStream())
            {
                DeobfuscatorContext.Assembly.Write(asm);
                files.Add(new ObfuscatedFile(newFileOptions, filesOptions.ModuleContext,
                                             filesOptions.AssemblyClientFactory, asm));

                using (var ms = new MemoryStream())
                {
                    new FilesDeobfuscator(filesOptions).doIt(ms, Rename);
                    DeobfuscatorContext.Assembly = AssemblyDef.Load(ms);
                }
            }
        }

        public override void CleanUp()
        {
           
        }
    }
}
