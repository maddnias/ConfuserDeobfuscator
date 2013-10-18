using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            string signature = FetchBlobSignature(asmDef.ManifestModule as ModuleDefMD) ??
                               FetchAttributeSignature(asmDef.ManifestModule);

            if (signature == null)
                throw new Exception("Failed to locate a valid confuser signature in the assembly");

            switch (signature)
            {
                case "Confuser v1.9.0.0":
                    return FetchSubversion1_9(asmDef);

                case "Confuser v1.8.0.0":
                    throw new Exception("This version is not supported yet");

                default:
                    throw new Exception(string.Format("Unable to create a deobfuscator for version: {0}",
                        signature));
            }
        }

        private static string FetchAttributeSignature(IHasCustomAttribute mod)
        {
            var confuserAttrib = mod.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "ConfusedByAttribute");
            if (confuserAttrib == null || !confuserAttrib.HasConstructorArguments)
                return null;
            return confuserAttrib.ConstructorArguments[0].Value.ToString();
        }

        private static string FetchBlobSignature(ModuleDefMD mod)
        {
            var blobStream = mod.BlobStream.ImageStream;
            var rawBuff = new byte[blobStream.Length];
            var exp = new Regex("Confuser v[0-9].[0-9].[0-9].[0-9]");

            blobStream.Read(rawBuff, 0, rawBuff.Length);

            var match = exp.Match(Encoding.UTF8.GetString(rawBuff));
            return match.Success ? match.Groups[0].Value : null;
        }

        private static DeobfuscatorBase FetchSubversion1_9(AssemblyDef asmDef)
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
