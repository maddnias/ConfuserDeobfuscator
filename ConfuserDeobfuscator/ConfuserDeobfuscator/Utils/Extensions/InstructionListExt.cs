using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class InstructionListExt 
    {
        public static Instruction FindInstruction(this IList<Instruction> instructions, Predicate<Instruction> pred, int index)
        {
            for (int i = 0, idx = 0; i < instructions.Count; i++)
                if (pred(instructions[i]))
                    if (idx++ == index)
                        return instructions[i];

            return null;
        }

        public static void Replace(this IList<Instruction> instrList, Instruction oldInstr, Instruction newInstr)
        {
            var idx = oldInstr.GetInstructionIndex(instrList);
            instrList.Insert(idx, newInstr);
            instrList.Remove(oldInstr);
        }

        public static T GetOperandAt<T>(this IList<Instruction> instructions, Predicate<Instruction> pred, int index)
        {
            for (int i = 0, x = 0; i < instructions.Count; i++)
                if (pred(instructions[i]))
                    if (x++ == index)
                        if (instructions[i].IsLdcI4())
                            return (T)Convert.ChangeType(instructions[i].GetLdcI4(), typeof(T));
                        else
                            return (T) instructions[i].Operand;

            return default(T);
        }

        public static Instruction FindInstruction(this IList<Instruction> instructions, Predicate<Instruction> pred, int index, int start)
        {
            for (int i = start, idx = 0; i < instructions.Count; i++)
                if (pred(instructions[i]))
                    if (idx++ == index)
                        return instructions[i];

            return null;
        }

        
    }
}
