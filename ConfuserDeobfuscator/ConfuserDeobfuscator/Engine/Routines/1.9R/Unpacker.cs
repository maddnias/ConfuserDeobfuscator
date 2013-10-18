using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Engine.Routines.Generic;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using Defuser.Utilities.Compression.LZMA;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    class Unpacker : DeobfuscationRoutine19R, IMetadataWorker, IFileRewriter
    {
        public ModuleDefMD ModMD { get; set; }

        public override string Title
        {
            get { return "Unpacking assembly"; }
        }

        public override bool Detect()
        {
            if (ModMD.TablesStream.FileTable.Rows == 0)
            {
                Ctx.UIProvider.WriteVerbose("No packer?");
                return false;
            }

            var fileRow = ModMD.TablesStream.ReadFileRow(1);
            if (ModMD.StringsStream.ReadNoNull(fileRow.Name) != "___.netmodule")
            {
                Ctx.UIProvider.WriteVerbose("No packer?");
                return false;
            }

            return true;
        }

        public override void Initialize()
        {
            ModMD = Ctx.OriginalMD;
        }

        public override void Process()
        {
            var origAsm = Ctx.Assembly;
            UnpackAllAssemblies(origAsm);
            Ctx.Assembly = origAsm;
           
            var mainRes = Ctx.Assembly.ManifestModule.Resources.FirstOrDefault(x => x.IsPrivate);
            if (mainRes == null)
                return;
            var decryptor = Ctx.Assembly.ManifestModule.EntryPoint.DeclaringType.FindMethod("smethod_3");

            var key = RetrieveDecryptionKey(decryptor);
            var asmDat = Decrypt((mainRes as EmbeddedResource).GetResourceData(), key);
            var mod = ModuleDefMD.Load(asmDat);

            mod.Name = Ctx.Assembly.ManifestModule.Name;
            mod.Kind = Ctx.Assembly.ManifestModule.Kind;
            mod.Assembly = Ctx.Assembly;

            Ctx.IsUnpacked = true;
            Ctx.Assembly.Modules[0] = mod;
            Ctx.OriginalMD = Ctx.Assembly.Modules[0] as ModuleDefMD;
            Ctx.UIProvider.WriteVerbose("Unpacked: {0}", 2, true, Ctx.Assembly.Name + Ctx.Assembly.GetExtension());
        }

        public override void CleanUp()
        {
        }

        static byte[] Decrypt(byte[] asm, int key0I)
        {
            byte[] dat;
            byte[] iv;
            byte[] key;
            using (var rdr = new BinaryReader(new MemoryStream(asm)))
            {
                dat = rdr.ReadBytes(rdr.ReadInt32());
                iv = rdr.ReadBytes(rdr.ReadInt32());
                key = rdr.ReadBytes(rdr.ReadInt32());
            }
            var key0 = key0I;
            for (var j = 0; j < key.Length; j += 4)
            {
                key[j + 0] ^= (byte)((key0 & 0x000000ff) >> 0);
                key[j + 1] ^= (byte)((key0 & 0x0000ff00) >> 8);
                key[j + 2] ^= (byte)((key0 & 0x00ff0000) >> 16);
                key[j + 3] ^= (byte)((key0 & 0xff000000) >> 24);
            }
            var rijn = new RijndaelManaged();
            using (var s = new CryptoStream(new MemoryStream(dat), rijn.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                var l = new byte[4];
                s.Read(l, 0, 4);
                var len = BitConverter.ToUInt32(l, 0);

                var decoder = new Lzma.LzmaDecoder();
                var prop = new byte[5];
                s.Read(prop, 0, 5);
                decoder.SetDecoderProperties(prop);
                long outSize = 0;
                for (var i = 0; i < 8; i++)
                {
                    var v = s.ReadByte();
                    if (v < 0)
                        throw (new Exception("Can't Read 1"));
                    outSize |= ((long)(byte)v) << (8 * i);
                }
                var ret = new byte[outSize];
                long compressedSize = len - 13;
                decoder.Code(s, new MemoryStream(ret, true), compressedSize, outSize);

                return ret;
            }
        }

        void UnpackAllAssemblies(AssemblyDef asm)
        {
            foreach (var res in asm.ManifestModule.Resources)
            {
                if (!(res is EmbeddedResource))
                    continue;
                if (!res.IsPrivate)
                    continue;
                var _res = res as EmbeddedResource;
                byte[] decAsm;
                if (StaticDecryptAssembly(asm, _res.GetResourceData(), out decAsm))
                {
                    try
                    {
                        var tmpAsm = Ctx.Deobfuscator.DeobfuscateAssembly(AssemblyDef.Load(decAsm));
                        tmpAsm.Write(Path.GetDirectoryName(Ctx.Filename) + "\\" + tmpAsm.Name + tmpAsm.GetExtension());

                        Ctx.UIProvider.WriteVerbose("Unpacked: {0}", 2, true, tmpAsm.Name + tmpAsm.GetExtension());

                        //var tester = new Unpacker();
                        //Ctx.Assembly = tmpAsm;
                        //tester.Initialize();
                        //if (tester.Detect())
                        //    UnpackAllAssemblies(tmpAsm);
                    }
                    catch (BadImageFormatException)
                    {

                    }
                }
            }
        }

        bool StaticDecryptAssembly(AssemblyDef asmDef, byte[] asm, out byte[] decData)
        {
            var decryptor = asmDef.ManifestModule.EntryPoint.DeclaringType.FindMethod("smethod_3");
            var key = RetrieveDecryptionKey(decryptor);
            decData = new byte[asm.Length];

            try
            {
                decData = Decrypt(asm, key);
            }
            catch
            {
                return false;
            }

            return true;
        }

        int RetrieveDecryptionKey(MethodDef decryptor)
        {
            CalculateMutations(decryptor);
            return decryptor.Body.Instructions.FindInstruction(x => x.IsLdcI4(), 0).GetLdcI4Value();
        }

        public void ReloadFile()
        {
            Ctx.Deobfuscator.FinalizeDeobfuscation();
            var finalName = Path.GetDirectoryName(Ctx.Filename) + "\\" +
                            Path.GetFileNameWithoutExtension(Ctx.Filename) + "_cleaned" +
                            Path.GetExtension(Ctx.Filename);
            Ctx.Filename = finalName;
            Ctx.Assembly = AssemblyDef.Load(finalName);
            Ctx.OriginalMD = Ctx.Assembly.ManifestModule as ModuleDefMD;
        }
    }
}
