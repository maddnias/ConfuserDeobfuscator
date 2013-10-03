using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Utils;
using dnlib.DotNet;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;
namespace ConfuserDeobfuscator.Engine.Routines._1._8
{
    public class ConstantsDecryption : DeobfuscationRoutine18
    {
        public override Dictionary<string, object> RoutineVariables { get; set; }

        public override string Title
        {
            get { return "Decrypting and restoring constants"; }
        }

        public override bool Detect()
        {
            return Ctx.Assembly.FindMethods(IsStringDecryptor).Any();
        }

        public override void Initialize()
        {
            RoutineVariables.Add("constStream", null);
        }

        public override void Process()
        {
   
        }

        public override void CleanUp()
        {
        
        }

        private bool IsStringDecryptor(MethodDef mDef)
        {
            if (!mDef.HasBody || !mDef.Body.HasExceptionHandlers)
                return false;

            if (mDef.Body.ExceptionHandlers.Count != 1)
                return false;

            if (mDef.Parameters.Count != 2 || mDef.ReturnType.TypeName != "Object")
                return false;

            if (!mDef.IsPrivate || !mDef.IsStatic)
                return false;

            return true;
        }

        private object StaticDecryptConstant(uint a, uint b, int mTok, int mtTok)
        {
            object ret = null;
            var x = (uint) (mTok ^ (mtTok*a));
            var h = 0x67452301 ^ x;
            uint h1 = 0x3bd523a0;
            uint h2 = 0x5f6f36c0;
            for (uint i = 1; i <= 64; i++)
            {
                h = (h & 0x00ffffff) << 8 | ((h & 0xff000000) >> 24);
                var n = (h & 0xff)%64;
                if (n < 16)
                {
                    h1 |= (((h & 0x0000ff00) >> 8) & ((h & 0x00ff0000) >> 16)) ^ (~h & 0x000000ff);
                    h2 ^= (h*i + 1)%16;
                    h += (h1 | h2) ^ 12345678;
                }
                else if (n >= 16 && n < 32)
                {
                    h1 ^= ((h & 0x00ff00ff) << 8) ^ (((h & 0x00ffff00) >> 8) | (~h & 0x0000ffff));
                    h2 += (h*i)%32;
                    h |= (h1 + ~h2) & 12345678;
                }
                else if (n >= 32 && n < 48)
                {
                    h1 += ((h & 0x000000ff) | ((h & 0x00ff0000) >> 16)) + (~h & 0x000000ff);
                    h2 -= ~(h + n)%48;
                    h ^= (h1%h2) | 12345678;
                }
                else if (n >= 48 && n < 64)
                {
                    h1 ^= (((h & 0x00ff0000) >> 16) | ~(h & 0x0000ff))*(~h & 0x00ff0000);
                    h2 += (h ^ i - 1)%n;
                    h -= ~(h1 ^ h2) + 12345678;
                }
            }
            var pos = h ^ b;
            byte type;
            byte[] bs;
            lock (RoutineVariables["constStream"])
            {
                var rdr = new BinaryReader(RoutineVariables["constStream"] as Stream);
                rdr.BaseStream.Seek(pos, SeekOrigin.Begin);
                type = rdr.ReadByte();
                bs = rdr.ReadBytes(rdr.ReadInt32());
            }

            byte[] f;
            int len;
            //var key = Assembly.GetCallingAssembly().GetModule(method.Module.ScopeName).ResolveSignature((int)(0x263013d3 ^ mTok));
            byte[] key = new byte[2];
            using (BinaryReader r = new BinaryReader(new MemoryStream(bs)))
            {
                len = r.ReadInt32() ^ 0x57425674;
                f = new byte[(len + 7) & ~7];
                for (int i = 0; i < f.Length; i++)
                {
                    int count = 0;
                    int shift = 0;
                    byte c;
                    do
                    {
                        c = r.ReadByte();
                        count |= (c & 0x7F) << shift;
                        shift += 7;
                    } while ((c & 0x80) != 0);

                    //count = PlaceHolder(count);
                    f[i] = (byte) (count ^ key[i%8]);
                }
            }
            if (type == 11)
                ret = BitConverter.ToDouble(f, 0);
            else if (type == 22)
                ret = BitConverter.ToSingle(f, 0);
            else if (type == 33)
                ret = BitConverter.ToInt32(f, 0);
            else if (type == 44)
                ret = BitConverter.ToInt64(f, 0);
            else if (type == 55)
                ret = Encoding.UTF8.GetString(f, 0, len);

            return ret;
        }
    }
}
