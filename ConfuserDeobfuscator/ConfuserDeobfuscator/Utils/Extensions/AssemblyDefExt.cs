using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Utils.Extensions
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

        public static TypeDef GetRootType(this AssemblyDef asmDef)
        {
            var mod = (asmDef.ManifestModule as ModuleDefMD);
            var rootTypeName = mod.StringsStream.Read(mod.TablesStream.ReadTypeDefRow(1).Name).String;

            if (rootTypeName == null)
                throw new Exception("Failed to read root type name from #Strings stream");

            var rootType = asmDef.ManifestModule.Types.FirstOrDefault(t => t.Name == rootTypeName);

            if (rootType == null)
                throw new Exception("Could not find root type");

            return rootType;
        }

        public static string GetExtension(this AssemblyDef asmDef)
        {
            var md = (asmDef.ManifestModule as ModuleDefMD);
            if (md == null)
                return null;
            
            var name = md.StringsStream.ReadNoNull(md.TablesStream.ReadModuleRow(1).Name);
            return Path.GetExtension(name);
        }
    }
}
