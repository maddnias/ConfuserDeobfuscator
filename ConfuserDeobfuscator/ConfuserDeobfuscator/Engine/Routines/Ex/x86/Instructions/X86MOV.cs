using System.Collections.Generic;
using Bea;
using ConfuserDeobfuscator.Utils.Extensions;

namespace ConfuserDeobfuscator.Engine.Routines.Ex.x86.Instructions
{
    internal class X86MOV : X86Instruction
    {
        public X86MOV(Disasm rawInstruction) : base()
        {
            Operands = new IX86Operand[2];
            Operands[0] = rawInstruction.Argument1.GetOperand();
            Operands[1] = rawInstruction.Argument2.GetOperand();
        }

        public override X86OpCode OpCode
        {
            get { return X86OpCode.MOV; }
        }

        public override void Execute(Dictionary<string, int> registers, Stack<int> localStack)
        {
            if (Operands[1] is X86ImmediateOperand)
                registers[((X86RegisterOperand) Operands[0]).Register.ToString()] =
                    (Operands[1] as X86ImmediateOperand).Immediate;
            else
                registers[((X86RegisterOperand) Operands[0]).Register.ToString()] =
                    registers[(Operands[1] as X86RegisterOperand).Register.ToString()];
        }
    }
}
