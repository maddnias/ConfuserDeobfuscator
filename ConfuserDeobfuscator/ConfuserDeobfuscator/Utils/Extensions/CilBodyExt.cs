using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class CilBodyExt
    {
        public static IEnumerable<Instruction> GetInstructionsBetween(this CilBody body,
                                                                Instruction init, Instruction ender)
        {
            var curInstr = init;
            while (curInstr.Next(body) != null && curInstr.Next(body) != ender)
            {
                yield return curInstr;
                curInstr = curInstr.Next(body);
            }
            yield return curInstr;
            yield return curInstr.Next(body);
        }
    }
}
