using System;
using System.Collections.Generic;
using ConfuserDeobfuscator.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class MethodDefExt
    {
        public static IEnumerable<Tuple<Instruction, MethodDef>> FindAllReferences(this MethodDef mDef)
        {
            foreach (
                var method in
                    mDef.Module.Assembly.FindMethods(x => x.HasBody))
            {
                for (var i = 0; i < method.Body.Instructions.Count; i++)
                {
                    if (method.Body.Instructions[i].IsCall())
                        if ((method.Body.Instructions[i].Operand as MethodSpec) != null)
                        {
                            var a = method.Body.Instructions[i].Operand as MethodSpec;
                            if (a.Method == mDef)
                                yield return Tuple.Create(method.Body.Instructions[i], method);
                        }
                        else if ((method.Body.Instructions[i].Operand as MethodDef) != null)
                        {
                            var a = method.Body.Instructions[i].Operand as MethodDef;
                            if (a == mDef)
                                yield return Tuple.Create(method.Body.Instructions[i], method);
                        }
                        else if ((method.Body.Instructions[i].Operand as MemberRef != null))
                        {
                            var a = method.Body.Instructions[i].Operand as MemberRef;
                            if (a.ResolveMethod() != null)
                                yield return Tuple.Create(method.Body.Instructions[i], method);
                        }
                }
            }
        }

        public static byte[] ReadBodyFromRva(this MethodDef method)
        {
            var stream = DeobfuscatorContext.OriginalMD.MetaData.PEImage.CreateFullStream();
            var offset = DeobfuscatorContext.OriginalMD.MetaData.PEImage.ToFileOffset(method.RVA);
            var nextMethod = DeobfuscatorContext.OriginalMD.TablesStream.ReadMethodRow(method.Rid + 1);
            var size = DeobfuscatorContext.OriginalMD.MetaData.PEImage.ToFileOffset((RVA)nextMethod.RVA) - offset;
            var buff = new byte[(size-20 < 0 ? 50 : size-20)];

            stream.Position = (long) offset + 20;
            stream.Read(buff, 0, buff.Length);
            return buff;
        }
    }
}
