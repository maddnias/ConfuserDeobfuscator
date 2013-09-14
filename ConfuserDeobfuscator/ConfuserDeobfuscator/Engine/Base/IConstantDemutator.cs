using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Base
{
    public interface IConstantDemutator
    {
        int[] DemutatedInts { get; set; }
        long[] DemutatedLongs { get; set; }
        void CalculateMutations(MethodDef method);

    }
}
