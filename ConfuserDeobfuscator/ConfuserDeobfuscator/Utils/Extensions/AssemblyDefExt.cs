using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Utils
{
    public static class AssemblyDefExt
    {
        public static MethodDef FindMethod(this AssemblyDef asmDef, Predicate<MethodDef> pred)
        {
            return (from modDef in asmDef.Modules from typeDef in modDef.Types from mDef in typeDef.Methods where mDef.HasBody select mDef).FirstOrDefault(mDef => pred(mDef));
        }

        public static IEnumerable<MethodDef> FindMethods(this AssemblyDef asmDef, Predicate<MethodDef> pred)
        {
            foreach (var tDef in asmDef.Modules[0].Types)
            {
                foreach (var mDef in from nt in tDef.NestedTypes from mDef in nt.Methods where pred(mDef) select mDef)
                    yield return mDef;

                foreach (var mDef in tDef.Methods.Where(mDef => pred(mDef)))
                    yield return mDef;
            }
        }
    }
}
