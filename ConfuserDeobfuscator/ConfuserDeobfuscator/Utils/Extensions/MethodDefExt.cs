using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

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
    }
}
