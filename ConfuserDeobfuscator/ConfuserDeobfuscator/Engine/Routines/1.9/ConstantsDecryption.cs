using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    public class ConstantsDecryption : DeobfuscationRoutine19R
    {
        public override string Title
        {
            get { return "Decrypting and restoring constants"; }
        }


        public class Decryptor
        {
            public byte[] ConstBuffer;

            public ulong Key0L { get; set; }
            public ulong Key1L { get; set; }
            public ulong Key2L { get; set; }
            public ulong Hash = 0xCBF29CE484222325;

            public int Key0I { get; set; }
            public int Key0D { get; set; }
            public int Token1 { get; set; }
            public int Token2 { get; set; }

            public Decryptor()
            {
                
            }

            public Decryptor(byte[] constBuffer)
            {
                ConstBuffer = constBuffer;
            }

            public T Decrypt<T>(uint mdModifier, ulong hashModifier)
            {
                object ret;
                var token = (uint)(Token2 * mdModifier);

                var _Key0L = Key0L;
                var _Key1L = Key1L;
                var _Key2L = Key2L;
                var _hash = Hash;

                _Key0L *= token;
                _Key1L = _Key1L*_Key0L;
                _Key2L = _Key2L*_Key0L;
                _Key0L = _Key0L*_Key0L;

                while (_Key0L != 0)
                {
                    _hash *= 0x100000001B3;
                    _hash = (_hash ^ _Key0L) + (_Key1L ^ _Key2L)*(uint) Key0I;
                    _Key1L *= 0x811C9DC5;
                    _Key2L *= 0xA2CEBAB2;
                    _Key0L >>= 8;
                }

                var dat = _hash ^ hashModifier;
                var pos = (uint) (dat >> 32);
                var len = (uint) dat;

                var bs = new byte[len];
                Array.Copy(ConstBuffer, (int) pos, bs, 0, len);
                var key = BitConverter.GetBytes(Token1 ^ Key0D);
                for (var i = 0; i < bs.Length; i++)
                    bs[i] ^= key[(pos + i)%4];

                if (typeof (T) == typeof (string))
                    ret = Encoding.UTF8.GetString(bs);
                else
                    return default(T);

                return (T) ret;
            }
        }
        public class ResourceDecryptor
        {
            public byte[] ConstBuffer { get; set; }

            public int Key0I { get; set; }
            public int Key0D { get; set; }
            public int Token1 { get; set; }

            public void DecryptResource(Stream stream, byte[] key)
            {
                var s = new MemoryStream();
               // var asm = System.Reflection.Assembly.LoadFile(Ctx.Filename);
                //var x = asm.GetManifestResourceStream(Encoding.UTF8.GetString(BitConverter.GetBytes(Key0I)));
                if (stream != null)
                {
                    var buff = new byte[stream.Length];
                    stream.Read(buff, 0, buff.Length);

                    //var key = asm.Modules.ToList()[0].ResolveSignature(Key0D ^ Token1);

                    var seed = BitConverter.ToUInt32(key, 0xc)*(uint) Key0I;
                    var _m = (ushort) (seed >> 16);
                    var _c = (ushort) (seed & 0xffff);
                    var m = _c;
                    var c = _m;
                    for (var i = 0; i < buff.Length; i++)
                    {
                        buff[i] ^= (byte) ((seed*m + c)%0x100);
                        m = (ushort) ((seed*m + _m)%0x10000);
                        c = (ushort) ((seed*c + _c)%0x10000);
                    }

                    var str = new DeflateStream(new CryptoStream(new MemoryStream(buff),
                                                                 new RijndaelManaged().CreateDecryptor(key,
                                                                                                       MD5.Create()
                                                                                                          .ComputeHash(
                                                                                                              key)),
                                                                 CryptoStreamMode.Read), CompressionMode.Decompress);
                    {
                        var dat = new byte[0x1000];
                        var read = str.Read(dat, 0, 0x1000);
                        do
                        {
                            s.Write(dat, 0, read);
                            read = str.Read(dat, 0, 0x1000);
                        } while (read != 0);
                    }
                }
                else
                    throw new NullReferenceException("Resource not found");

                ConstBuffer = s.ToArray();
            }
        }

        //TODO: Implement proper detection
        public override bool Detect()
        {
            return Ctx.Assembly.FindMethods(m => IsStringDecryptor(m, false)).Count() != 0;
        }

        public override void Initialize()
        {
           
        }

        public override void Process()
        {
            var allDecryptors = Ctx.Assembly.FindMethods(m => IsStringDecryptor(m, true)).ToList();
            var usedDecryptors = Ctx.Assembly.FindMethods(m => IsStringDecryptor(m, false)).ToList();
            var demutatedDecryptors = new List<Tuple<Decryptor, MethodDef, List<Tuple<Instruction, MethodDef>>>>();

            RoutineVariables.Add("decryptors", allDecryptors);

            usedDecryptors.ForEach(d =>
                                       {
                                           CalculateMutations(d);
                                           var refs = d.FindAllReferences().ToList();
                                           demutatedDecryptors.Add(Tuple.Create(new Decryptor(), d, refs));
                                       });

            DecryptConstantsBuffer(demutatedDecryptors);
            DecryptConstants(demutatedDecryptors);
        }

        public override void CleanUp()
        {
            var cctor = RoutineVariables["cctor"] as MethodDef;
            var decryptors = RoutineVariables["decryptors"] as List<MethodDef>;
            var constTbl = RoutineVariables["constTbl"] as FieldDef;
            var constBuffer = RoutineVariables["constBuffer"] as FieldDef;
            var body = RoutineVariables["resDecryptor"] as Tuple<List<Instruction>, CilBody>;
            var res = RoutineVariables["badResource"] as EmbeddedResource;

            if (body != null)
            {
                body.Item1.Insert(0, body.Item1[0].Previous(body.Item2));
                foreach (var instr in body.Item1)
                    cctor.Body.Instructions.Remove(instr);
                Ctx.UIProvider.WriteVerbose("Removed resource decryptor from {0}::{1}", 2, true, cctor.DeclaringType.Name,
                                            cctor.Name);
            }

            if (decryptors != null)
            {
                foreach (var dec in decryptors)
                {
                    Ctx.UIProvider.WriteVerbose("Removed constants decryptor type: {0}", 2, true, dec.DeclaringType.Name); 
                    Ctx.Assembly.ManifestModule.Types.Remove(dec.DeclaringType);
                }
            }

            if (constTbl != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed constTbl field from {0}::{1}", 2, true, constTbl.DeclaringType.Name,
                                            constTbl.Name);
                constTbl.DeclaringType.Fields.Remove(constTbl);
            }

            if (constBuffer != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed constBuffer field from {0}::{1}", 2, true, constBuffer.DeclaringType.Name,
                                            constBuffer.Name);
                constBuffer.DeclaringType.Fields.Remove(constBuffer);
            }

            if (res != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed bad resource: {0}", 2, true, res.Name);
                Ctx.Assembly.ManifestModule.Resources.Remove(res);
            }
        }

        private void DecryptConstantsBuffer(List<Tuple<Decryptor, MethodDef,
                                            List<Tuple<Instruction, MethodDef>>>> demutatedDecryptors)
        {
            var resDec = new ResourceDecryptor();
            var cctor = Ctx.Assembly.ManifestModule.Types.FirstOrDefault(x => x.Name == "<Module>").GetStaticConstructor();
            if (cctor == null)
                return;
            var constTbl =
                cctor.DeclaringType.Fields.First(x => x.FieldType.FullName.EndsWith("Dictionary`2<System.UInt32,System.Object>"));
            var constBuffer =
                cctor.DeclaringType.Fields.First(x => x.FieldType.FullName.EndsWith("System.Byte[]"));

            RoutineVariables.Add("cctor", cctor);
            RoutineVariables.Add("constTbl", constTbl);
            RoutineVariables.Add("constBuffer", constBuffer);

            CalculateMutations(cctor);

            Instruction initInstr = null;
            Instruction endInstr = null;

            foreach (var instr in cctor.Body.Instructions)
            {
                if (instr.OpCode.OperandType != OperandType.InlineField)
                    continue;
                if (instr.Operand == constTbl)
                    initInstr = instr;
                else if (instr.Operand == constBuffer)
                    endInstr = instr;
            }

            var parsedBody = cctor.Body.GetInstructionsBetween(initInstr, endInstr).ToList();
            RoutineVariables.Add("resDecryptor", Tuple.Create(parsedBody, cctor.Body));
            resDec.Key0I = parsedBody.GetOperandAt<int>(x => x.IsLdcI4(), 0);
            resDec.Key0D = parsedBody.FindInstruction(x => x.IsCall() && x.Operand.ToString().EndsWith("MemberInfo::get_Module()"), 0).Next(cctor.Body).GetLdcI4Value();
            resDec.Token1 = cctor.MDToken.ToInt32();

            var defMd = Ctx.Assembly.ManifestModule as ModuleDefMD;
            if (defMd != null)
            {
                var key = defMd.ReadBlob((uint)(resDec.Key0D ^ resDec.Token1));
                if (key.Length != 32) // Doesn't read same signature before and after anti-tamper
                    key = Ctx.OriginalMD.ReadBlob(resDec.Key0D.GetUInt() ^ Ctx.OriginalMD.ResolveMethod(1).MDToken.ToUInt32()); // .cctor should be at RID 1

                var resname = Encoding.UTF8.GetString(BitConverter.GetBytes(resDec.Key0I));
                var res = Ctx.Assembly.ManifestModule.Resources.FirstOrDefault(x => Encoding.UTF8.GetBytes(x.Name).SequenceEqual(Encoding.UTF8.GetBytes(resname)));
                if (res == null)
                    res = Ctx.Assembly.ManifestModule.Resources.First(x => x.Name.String == resname);
                RoutineVariables.Add("badResource", res);
                var resource = res as EmbeddedResource;
                if (resource != null)
                    resDec.DecryptResource(resource.GetResourceStream(), key);
            }

            demutatedDecryptors.ForEach(d => d.Item1.ConstBuffer = resDec.ConstBuffer);
        }

        private void DecryptConstants(
            List<Tuple<Decryptor, MethodDef, List<Tuple<Instruction, MethodDef>>>> demutatedDecryptors)
        {
            demutatedDecryptors.ForEach(d =>
            {
                CalculateMutations(d.Item2);
                PopulateKeys(d.Item1, d.Item2);

                foreach (var @ref in d.Item3)
                {
                    //var tracer = new ILEmulator(@ref.Item2.Body,
                    //                            @ref.Item2.Body.Instructions.Count);
                    //var test = tracer.TraceCallParameters(@ref.Item1).ToList();
                    var body = @ref.Item2.Body;
                    var mdModifier =
                        (uint) @ref.Item1.Previous(body).Previous(body).GetLdcI4Value();
                    var hashModifier = (ulong) @ref.Item1.Previous(body).GetLdcI8();
                    var str = d.Item1.Decrypt<string>(mdModifier, hashModifier);

                    body.SimplifyMacros(@ref.Item2.Parameters);
                    body.SimplifyBranches();
                    body.Instructions.Insert(
                        body.Instructions.IndexOf(@ref.Item1),
                        Instruction.Create(OpCodes.Ldstr, str));
                    body.Instructions.RemoveAt(body.Instructions.IndexOf(@ref.Item1) - 2);
                    body.Instructions.RemoveAt(body.Instructions.IndexOf(@ref.Item1) - 1);
                    body.Instructions.Remove(@ref.Item1);
                    body.OptimizeMacros();
                    body.OptimizeBranches();

                    Ctx.UIProvider.WriteVerbose("Restored string \"{0}\"", 2, true, str);
                }
            });
        }

        private static void PopulateKeys(Decryptor decryptor, MethodDef method)
        {
            decryptor.Token2 = method.DeclaringType.MDToken.ToInt32();
            decryptor.Token1 = method.MDToken.ToInt32();
            decryptor.Key0L =  method.Body.Instructions.GetOperandAt<long>(x => x.OpCode.Code == Code.Ldc_I8, 0).GetULong();
            decryptor.Key1L =  method.Body.Instructions.GetOperandAt<long>(x => x.OpCode.Code == Code.Ldc_I8, 1).GetULong();
            decryptor.Key2L =  method.Body.Instructions.GetOperandAt<long>(x => x.OpCode.Code == Code.Ldc_I8, 2).GetULong();
            decryptor.Key0I = method.Body.Instructions.GetOperandAt<int>(x => x.IsLdcI4(), 0);
            decryptor.Key0D =
                method.Body.Instructions.FindInstruction(
                    x => x.IsCall() && x.Operand.ToString().EndsWith("get_MetadataToken()"), 1)
                      .Next(method.Body)
                      .GetLdcI4Value();
        }
        private static bool IsStringDecryptor(MethodDef mDef, bool all)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.Parameters.Count == 0 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Parameters.Count != 2 || !(mDef.Body.Variables.Count != 15 || mDef.Body.Variables.Count != 16))
                return false;

            if (mDef.Body.Variables.FirstOrDefault(x => x.Type.TypeName.EndsWith("MethodBase")) == null)
                return false;

            return all || mDef.FindAllReferences().Any();
        }
    }
}
