using System.Collections.Generic;
using System.IO;
using ConfuserDeobfuscator.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Deobfuscators.Base
{
    public abstract class DeobfuscatorBase
    {
        public abstract Dictionary<string, Pipeline> Pipelines { get; set; }

        public abstract void InitializePipelines();
        public abstract void ProcessFile(string filename);
        public virtual AssemblyDef DeobfuscateAssembly(AssemblyDef asmDef) { return null; }
        public abstract void DeobfuscateAssembly(string filename);

        public virtual void FinalizeDeobfuscation()
        {
            var filename = Ctx.Filename;
            var finalName = Path.GetDirectoryName(filename) + "\\" +
                            Path.GetFileNameWithoutExtension(filename) + "_cleaned" +
                            Path.GetExtension(filename);

            Ctx.Assembly.Write(finalName);//, new ModuleWriterOptions { Logger = DummyLogger.NoThrowInstance});
            Ctx.UIProvider.Write("_______________________________________\n\nSaved deobfuscated assembly at {0}", 0, true, finalName);
        }
    }
}
