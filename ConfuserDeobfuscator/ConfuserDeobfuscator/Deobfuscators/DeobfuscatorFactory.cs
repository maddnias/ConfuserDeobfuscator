using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Deobfuscators.Base;
using ConfuserDeobfuscator.Utils;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Deobfuscators
{
    public static class DeobfuscatorFactory
    {
        public static DeobfuscatorBase CreateDeobfuscator(string assembly)
        {
            var asmDef = AssemblyDef.Load(assembly);
            var confuserAttrib =
                asmDef.ManifestModule.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "ConfusedByAttribute");

            if (confuserAttrib == null)
                throw new Exception("Assembly does not contain a confuser signature");

            switch (confuserAttrib.ConstructorArguments[0].Value.ToString())
            {
                case "Confuser v1.9.0.0":
                    return FetchSubversion(asmDef);

                case "Confuser v1.8.0.0":
                    throw new Exception("This version is not supported yet");

                default:
                    throw new Exception(string.Format("Unable to create a deobfuscator for version: {0}",
                        confuserAttrib.ConstructorArguments[0].Value));
            }
        }

        private static DeobfuscatorBase FetchSubversion(AssemblyDef asmDef)
        {
            foreach (var type in asmDef.ManifestModule.Types)
            {
                if (type.Name.String.StartsWith("Lzma") && type.HasNestedTypes && type.NestedTypes.Count == 6)
                {
                    // Weak 1.9L detection
                    return Activator.CreateInstance<Deobfuscator19L>();
                }
            }

            return Activator.CreateInstance<Deobfuscator19R>();
        }
    }
}
