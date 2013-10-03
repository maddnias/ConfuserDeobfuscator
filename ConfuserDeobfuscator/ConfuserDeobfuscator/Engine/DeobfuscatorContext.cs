using System.Collections.Generic;
using ConfuserDeobfuscator.Deobfuscators;
using ConfuserDeobfuscator.Deobfuscators.Base;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine
{
    public static class DeobfuscatorContext
    {
        public enum OutputLevel
        {
            None = 0,
            Normal = 1,
            Verbose = 2
        }

        public static DeobfuscatorBase Deobfuscator { get; set; }
        public static IUserInterfaceProvider UIProvider { get; set; }
        public static AssemblyDef Assembly { get; set; }
        public static string Filename { get; set; }
        public static Dictionary<string, object> GlobalVariables { get; set; }
        public static OutputLevel LoggingLevel { get; set; }
        public static bool IsUnpacked { get; set; }

        // For constantsdecryptor
        public static ModuleDefMD OriginalMD { get; set; }
    }
}
