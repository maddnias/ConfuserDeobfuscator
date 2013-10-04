using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    public class MethodDecryptor : DeobfuscationRoutine19R, IMetadataWorker, IFileRewriter
    {
        public ModuleDefMD ModMD { get; set; }

        private MemoryStream RestoredAssembly { get; set; }

        public override string Title
        {
            get { return "Decrypting and restoring methods"; }
        }

        public override bool Detect()
        {
            var antiTamper = Ctx.Assembly.FindMethod(IsAntiTamper);
            RoutineVariables.Add("antiTamper", antiTamper);
            var decryptor = Ctx.Assembly.FindMethod(IsDecryptor);

            if (antiTamper == null || decryptor == null)
            {
                Ctx.UIProvider.WriteVerbose("No anti-tamper?");
                return false;
            }

            RoutineVariables.Add("decryptor", decryptor);

            return true;
        }

        public override void Initialize()
        {
            ModMD = Ctx.OriginalMD;
            RestoredAssembly = new MemoryStream();
        }

        public override void Process()
        {
            var antiTamper = RoutineVariables["antiTamper"] as MethodDef;
            var decryptor = RoutineVariables["decryptor"] as MethodDef;

            CalculateMutations(antiTamper);
            CalculateMutations(decryptor);

            ReadKeys(antiTamper.Body, decryptor.Body);

            var badSection = ModMD.MetaData.PEImage.ImageSectionHeaders.Last();
            var stream = ModMD.MetaData.PEImage.CreateStream((FileOffset) badSection.PointerToRawData,
                                                             badSection.SizeOfRawData);

            byte[] iv, dat;
            ulong checksum;
            int ivLen;
            int datLen;
            ParseDecryptionData(stream, out checksum, out ivLen, out iv, out datLen, out dat);

            stream = ModMD.MetaData.PEImage.CreateFullStream();
            var buff = CreateStreamsBuffer(stream);
            var decData = DecryptData(buff, iv, dat);

            // Skip two first bytes (0xd6 & 0x6f)
            var tmp = new byte[decData.Length - 2];
            Buffer.BlockCopy(decData, 2, tmp, 0, tmp.Length);

            Array.Resize(ref decData, decData.Length - 2);
            Buffer.BlockCopy(tmp, 0, decData, 0, tmp.Length);

            using (var decStream = new MemoryStream(decData))
            {
               // using (var fileStream = new MemoryStream())
                {
                    try
                    {
                        DeobfuscatorContext.Assembly.Write(RestoredAssembly);
                    }
                    catch
                    {
                        var buff2 = File.ReadAllBytes(DeobfuscatorContext.Filename);
                        RestoredAssembly.Write(buff2, 0, buff2.Length);

                    }
                    RestoreBodies(decStream, RestoredAssembly);
                }
            }
        }

        public override void CleanUp()
        {
            // AntiTamperRemover.cs does the cleanup
        }

        private void RestoreBodies(Stream decStream, Stream fileStream)
        {
            using (var reader = new BinaryReader(decStream))
            {
                var len = reader.ReadUInt32();

                for (var i = 0; i < len; i++)
                {
                    var pos = reader.ReadUInt32() ^ DemutatedKeys["antitamper"].DemutatedUInts[0];
                    if (pos == 0) continue;
                    var rva = (reader.ReadUInt32() ^ DemutatedKeys["antitamper"].DemutatedUInts[1]);

                    var offset = ModMD.MetaData.PEImage.ToFileOffset((RVA)rva);
                    var cDat = reader.ReadBytes(reader.ReadInt32());

                    Ctx.UIProvider.WriteVerbose("Restored body with RVA 0x{0:X4} to 0x{1:X4}", 2, true, rva, (uint)offset);

                    fileStream.Position = (long)offset;
                    fileStream.Write(cDat, 0, cDat.Length);
                }
            }
        }

        private byte[] DecryptData(byte[] buff, byte[] iv, byte[] dat)
        {
            var ri = new RijndaelManaged();
            var ret = new byte[dat.Length];
            var ms = new MemoryStream(dat);
            using (
                var cStr = new CryptoStream(ms, ri.CreateDecryptor(SHA256.Create().ComputeHash(buff), iv),
                                            CryptoStreamMode.Read))
            {
                cStr.Read(ret, 0, dat.Length);
            }

            var sha = SHA512.Create();
            var c = sha.ComputeHash(buff);
            for (var i = 0; i < ret.Length; i += 64)
            {
                var len = ret.Length <= i + 64 ? ret.Length : i + 64;
                for (var j = i; j < len; j++)
                    ret[j] ^= (byte)(c[j - i] ^ DemutatedKeys["decryptor"].DemutatedInts[0]);
                c = sha.ComputeHash(ret, i, len - i);
            }

            return ret;
        }
        private byte[] CreateStreamsBuffer(IBinaryReader reader)
        {
            var tmpBuffer = new List<byte>();

            foreach (var stream in ModMD.MetaData.AllStreams)
            {
                var size = stream.StreamHeader.StreamSize;
                reader.Position = (long) stream.StartOffset;
                tmpBuffer.AddRange(reader.ReadBytes((int)size));
            }

            return tmpBuffer.ToArray();
        }
        private void ParseDecryptionData(IBinaryReader reader, out ulong checksum, out int ivLen, out byte[] iv,
                                         out int datLen, out byte[] dat)
        {
            checksum = reader.ReadUInt64() ^ DemutatedKeys["antitamper"].DemutatedULongs[0];
            reader.ReadInt32(); // sn
            reader.ReadInt32(); // snLen
            iv = reader.ReadBytes(ivLen = reader.ReadInt32() ^ DemutatedKeys["antitamper"].DemutatedInts[0]);
            dat = reader.ReadBytes(datLen = reader.ReadInt32() ^ DemutatedKeys["antitamper"].DemutatedInts[1]);
        }
        private void ReadKeys(CilBody atBody, CilBody decBody)
        {
            var antiTamperKeys = new DemutatedKeys();
            var decryptorKeys = new DemutatedKeys();

            var key0L =
                (long)atBody.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().Contains("ReadUInt64()"), 0).Next(atBody).Operand;

            var key0I =
                atBody.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().Contains("ReadInt32()"), 2).Next(atBody).GetLdcI4Value();

            var key1I =
                atBody.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().Contains("ReadInt32()"), 3).Next(atBody).GetLdcI4Value();

            var key2I =
                atBody.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().Contains("ReadUInt32()"), 10).Next(atBody).GetLdcI4Value();

            var key3I =
                atBody.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().Contains("ReadUInt32()"), 11).Next(atBody).GetLdcI4Value();

            var decKey0I =
                decBody.Instructions.FindInstruction(
                    x => x.OpCode.Code == Code.Ldelem_U1, 0).Next(decBody).GetLdcI4Value();

            antiTamperKeys.DemutatedInts = new[]
                                               {
                                                   key0I,   // IV
                                                   key1I    // dats
                                               };
            antiTamperKeys.DemutatedUInts = new[]
                                                {
                                                    key2I.GetUInt(),    // pos
                                                    key3I.GetUInt()     // RVA
                                                };

            antiTamperKeys.DemutatedULongs = new[]
                                                {
                                                    key0L.GetULong()    // checksum
                                                };

            decryptorKeys.DemutatedInts = new[]
                                              {
                                                  decKey0I,
                                              };

            //antiTamperKeys.DemutatedInts = new[]
            //                                   {
            //                                       atBody.Instructions.GetOperandAt<int>(x =>x.IsLdcI4(), 42),      // IV
            //                                       atBody.Instructions.GetOperandAt<int>(x => x.IsLdcI4(), 43),     // dats
            //                                       atBody.Instructions.GetOperandAt<int>(x => x.IsLdcI4(), 56),     // pos
            //                                       atBody.Instructions.GetOperandAt<int>(x => x.IsLdcI4(), 57),     // RVA
            //                                   };

            //antiTamperKeys.DemutatedLongs = new[]
            //                                    {
            //                                        atBody.Instructions.GetOperandAt<long>(x =>x.OpCode.Code == Code.Ldc_I8, 0) // Checksum
            //                                    };

            //decryptorKeys.DemutatedInts = new[]
            //                                  {
            //                                      decBody.Instructions.GetOperandAt<int>(x => x.IsLdcI4(), 7)       // Decryption key
            //                                  };

            DemutatedKeys.Add("antitamper", antiTamperKeys);
            DemutatedKeys.Add("decryptor", decryptorKeys);
        }
        public bool IsDecryptor(MethodDef mDef)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.ParamDefs.Count != 3 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Body.Variables.Count != 9 && mDef.Body.Variables.Count != 10)
                return false;

            if (mDef.FindAllReferences().Count() != 1)
                return false;

            if (mDef.FindAllReferences().ToList()[0].Item2 != RoutineVariables["antiTamper"])
                return false;

            return true;
        }
        public static bool IsAntiTamper(MethodDef mDef)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.ParamDefs.Count != 0 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Body.Variables.Count != 43 && mDef.Body.Variables.Count != 44)
                return false;

            if (
                mDef.Body.Instructions.FindInstruction(
                    i => i.IsCall() && i.Operand.ToString().Contains("GetHINSTANCE"), 0) == null)
                return false;

            return true;
        }

        public void ReloadFile()
        {
            //File.WriteAllBytes(Ctx.Filename + "UN", RestoredAssembly.ToArray());
            DeobfuscatorContext.Assembly = AssemblyDef.Load(RestoredAssembly);
        }
    }
}
