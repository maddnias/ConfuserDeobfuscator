using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Base
{
    interface IDelegateResolver
    {
        IEnumerable<Tuple<IMemberRef, FieldDef>> ResolveProxyMethod(TypeDef proxy, int key);
        int ReadKey(MethodDef proxyGenerator);
        void RestoreProxyCall(Tuple<TypeDef, IMemberRef, FieldDef> resolvedProxy);
        void RestoreProxyCall(Tuple<TypeDef, IMemberRef, FieldDef> resolvedProxy, char virtIdentifier);
    }
}
