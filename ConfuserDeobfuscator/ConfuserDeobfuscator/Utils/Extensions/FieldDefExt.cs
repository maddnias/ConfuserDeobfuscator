using System;
using System.Collections.Generic;
using ConfuserDeobfuscator.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class FieldDefExt
    {
        public static IEnumerable<Tuple<Instruction, MethodDef>> FindAllReferences(this FieldDef fld)
        {
            foreach (var mDef in DeobfuscatorContext.Assembly.FindMethods(m => m.HasBody))
                foreach (var instr in mDef.Body.Instructions)
                    if (instr.OpCode.OperandType == OperandType.InlineField)
                        if (instr.Operand == fld)
                            yield return Tuple.Create(instr, mDef);
        }
    }
}
