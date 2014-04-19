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
    class X86IMUL : X86Instruction
    {
        public X86IMUL(Disasm rawInstruction) : base()
        {
            Operands = new IX86Operand[3];
            Operands[0] = rawInstruction.Argument1.GetOperand();
            Operands[1] = rawInstruction.Argument2.GetOperand();
            Operands[2] = rawInstruction.Argument3.GetOperand();
        }

        public override X86OpCode OpCode { get { return X86OpCode.IMUL; } }

        public override void Execute(Dictionary<string, int> registers, Stack<int> localStack)
        {
            var source = ((X86RegisterOperand) Operands[0]).Register.ToString();
            var target1 = ((X86RegisterOperand) Operands[1]).Register.ToString();

            if (Operands[2] is X86ImmediateOperand)
                registers[source] = registers[target1]*((X86ImmediateOperand) Operands[2]).Immediate;
            else
                registers[source] = registers[target1]*registers[((X86RegisterOperand) Operands[2]).Register.ToString()];
        }
    }
}
