using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bea;
using ConfuserDeobfuscator.Utils.Extensions;

namespace ConfuserDeobfuscator.Engine.Routines.Ex.x86.Instructions
{
    class X86ADD : X86Instruction
    {
        public X86ADD(Disasm rawInstruction) : base()
        {
            Operands = new IX86Operand[2];
            Operands[0] = rawInstruction.Argument1.GetOperand();
            Operands[1] = rawInstruction.Argument2.GetOperand();
        }

        public override X86OpCode OpCode { get { return X86OpCode.ADD; } }

        public override void Execute(Dictionary<string, int> registers, Stack<int> localStack)
        {
            if (Operands[1] is X86ImmediateOperand)
                registers[((X86RegisterOperand) Operands[0]).Register.ToString()] +=
                    ((X86ImmediateOperand) Operands[1]).Immediate;
            else
                registers[((X86RegisterOperand) Operands[0]).Register.ToString()] +=
                    registers[((X86RegisterOperand) Operands[1]).Register.ToString()];
        }
    }
}
