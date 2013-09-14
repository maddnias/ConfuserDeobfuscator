using System.Collections.Generic;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine
{
    public static class DeobfuscatorContext
    {
        public enum OutputLevel
        {
            Normal = 0,
            Verbose = 1
        }

        public static IUserInterfaceProvider UIProvider { get; set; }
        public static AssemblyDef Assembly { get; set; }
        public static string Filename { get; set; }
        public static Dictionary<string, object> GlobalVariables { get; set; }
        public static OutputLevel LoggingLevel { get; set; }

        // For constantsdecryptor
        public static ModuleDefMD OriginalMD { get; set; }
    }
}
