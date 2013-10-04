using System.Collections.Generic;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Base
{
    public interface IConstantDemutator
    {
        Dictionary<string, DemutatedKeys> DemutatedKeys { get; set; }
        void CalculateMutations(MethodDef method);

    }
}
