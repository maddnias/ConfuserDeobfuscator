using System.Linq;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class TypeDefExt
    {
        public static MethodDef GetStaticConstructor(this TypeDef type)
        {
            if (type == null)
                return null;
            return type.Methods.FirstOrDefault(m => m.IsStaticConstructor && m.HasBody);
        }
    }
}
