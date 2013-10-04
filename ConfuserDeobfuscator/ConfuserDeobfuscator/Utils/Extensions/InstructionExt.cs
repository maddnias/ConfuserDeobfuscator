using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class InstructionExt
    {
        public static bool IsCall(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Call ||
                   instr.OpCode == OpCodes.Calli ||
                   instr.OpCode == OpCodes.Callvirt;
        }

        public static bool IsLdcI4WOperand(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldc_I4 ||
                   instr.OpCode == OpCodes.Ldc_I4_S;
        }

        public static bool IsLdcI8WOperand(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldc_I8;
        }

        public static bool IsLdcI4(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldc_I4 ||
                   instr.OpCode == OpCodes.Ldc_I4_0 ||
                   instr.OpCode == OpCodes.Ldc_I4_1 ||
                   instr.OpCode == OpCodes.Ldc_I4_2 ||
                   instr.OpCode == OpCodes.Ldc_I4_3 ||
                   instr.OpCode == OpCodes.Ldc_I4_4 ||
                   instr.OpCode == OpCodes.Ldc_I4_5 ||
                   instr.OpCode == OpCodes.Ldc_I4_6 ||
                   instr.OpCode == OpCodes.Ldc_I4_7 ||
                   instr.OpCode == OpCodes.Ldc_I4_8 ||
                   instr.OpCode == OpCodes.Ldc_I4_M1 ||
                   instr.OpCode == OpCodes.Ldc_I4_S;
        }

        public static bool IsTarget(this Instruction instr, CilBody body)
        {
            return body.Instructions.Where(OnFunc).Any(instr1 => instr1.Operand as Instruction == instr);
        }

        private static bool OnFunc(Instruction instr)
        {
            return instr.IsUnconditionalBranch();
        }

        public static Instruction Next(this Instruction instr, CilBody body)
        {
            return body.Instructions.FirstOrDefault(i => i.Offset == instr.Offset + instr.GetSize());
        }

        public static Instruction Previous(this Instruction instr, CilBody body)
        {
            return body.Instructions.FirstOrDefault(i => i.Next(body) == instr);
        }

        public static long GetLdcI8(this Instruction instr)
        {
            if (instr.OpCode.Code != Code.Ldc_I8)
                throw new InvalidProgramException("instr not long");

            return (long)instr.Operand;
        }

        public static bool FollowsPattern(
            this Instruction instr,
            CilBody body,
            out Instruction ender,
            List<Predicate<Instruction>> preds,
            int minPatternSize,
            out int patternSize)
        {
            var curInstr = instr;
            ender = null;
            var correct = 0;
            patternSize = 0;
            while (curInstr.Next(body) != null && preds.Any(p => p(curInstr.Next(body))))
            {
                curInstr = curInstr.Next(body);
                correct++;
            }
            if (correct >= minPatternSize)
            {
                patternSize = correct + 1;
                ender = curInstr;
                return true;
            }
            return false;
        }

        public static bool PreceedsPattern(this Instruction instr, CilBody body, Predicate<Instruction>[] preds)
        {
            var curInstr = instr;
            foreach (var p in preds)
            {
                if (curInstr.Next(body) != null && p(curInstr.Next(body)))
                    curInstr = curInstr.Next(body);
                else
                    return false;
            }
            return true;
        }

        public static int GetLdcI4(this Instruction instr)
        {
            switch (instr.OpCode.Code)
            {
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                    return Int32.Parse(instr.OpCode.Code.ToString().Split('_')[2]); // Lazy :)

                case Code.Ldc_I4_M1:
                    return -1;

                case Code.Ldc_I4:
                case Code.Ldc_I4_S:
                    return (int)Convert.ChangeType(instr.Operand, typeof(int)); // No idea why I have to cast it this way

                default:
                    throw new Exception("Internal invalid instruction!");
            }
        }

        public static bool IsStLoc(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Stloc ||
                   instr.OpCode == OpCodes.Stloc_0 ||
                   instr.OpCode == OpCodes.Stloc_1 ||
                   instr.OpCode == OpCodes.Stloc_2 ||
                   instr.OpCode == OpCodes.Stloc_3 ||
                   instr.OpCode == OpCodes.Stloc_S;

        }

        public static bool IsLdLoc(this Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldloc ||
                   instr.OpCode == OpCodes.Ldloc_0 ||
                   instr.OpCode == OpCodes.Ldloc_1 ||
                   instr.OpCode == OpCodes.Ldloc_2 ||
                   instr.OpCode == OpCodes.Ldloc_3 ||
                   instr.OpCode == OpCodes.Ldloc_S;

        }

        public static bool IsUnconditionalBranch(this Instruction instr)
        {
            return
                instr.OpCode == OpCodes.Br ||
                instr.OpCode == OpCodes.Br_S ||
                instr.OpCode == OpCodes.Leave_S ||
                instr.OpCode == OpCodes.Leave;
        }

        public static bool IsConditionalBranch(this Instruction instr)
        {
            return
                instr.OpCode == OpCodes.Brtrue ||
                instr.OpCode == OpCodes.Brtrue_S ||
                instr.OpCode == OpCodes.Brfalse ||
                instr.OpCode == OpCodes.Brfalse_S ||
                instr.OpCode == OpCodes.Ble ||
                instr.OpCode == OpCodes.Ble_S ||
                instr.OpCode == OpCodes.Ble_Un ||
                instr.OpCode == OpCodes.Ble_Un_S ||
                instr.OpCode == OpCodes.Blt ||
                instr.OpCode == OpCodes.Blt_S ||
                instr.OpCode == OpCodes.Blt_Un ||
                instr.OpCode == OpCodes.Blt_Un_S ||
                instr.OpCode == OpCodes.Bge ||
                instr.OpCode == OpCodes.Bge_S ||
                instr.OpCode == OpCodes.Bge_Un ||
                instr.OpCode == OpCodes.Bge_Un_S ||
                instr.OpCode == OpCodes.Beq ||
                instr.OpCode == OpCodes.Beq_S;
        }

        public static IEnumerable<Instruction> GetNextInstructions(this Instruction instr, CilBody body, int count)
        {
            var curInstr = instr;
            for (var i = 0; i < count; i++)
            {
                yield return curInstr;
                curInstr = curInstr.Next(body);
            }
        }

        public static int GetInstructionIndex(this Instruction instr, IList<Instruction> instructions)
        {
            var count = 0;

            while (instructions[count++] != instr)
                // ReSharper disable RedundantJumpStatement
                continue;
            // ReSharper restore RedundantJumpStatement

            return count;
        }

        public static IEnumerable<Instruction> FindAllReferences(this Instruction instr, CilBody body)
        {
            foreach (var @ref in body.Instructions.Where(x => (x.IsConditionalBranch() || x.IsBr())))
            {
                if ((@ref.Operand as Instruction) == instr)
                    yield return @ref;
            }
        }
    }
}
