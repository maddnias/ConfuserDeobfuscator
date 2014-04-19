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
    class X86POP : X86Instruction
    {
        public X86POP(Disasm rawInstruction) : base()
        {
            Operands = new IX86Operand[1];
            Operands[0] = rawInstruction.Argument1.GetOperand();
        }

        public override X86OpCode OpCode { get { return X86OpCode.POP; } }

        public override void Execute(Dictionary<string, int> registers, Stack<int> localStack)
        {
            // Pretend to pop stack
            if (localStack.Count < 1)
                return;

            registers[((X86RegisterOperand) Operands[0]).Register.ToString()] = localStack.Pop();
        }
    }
}
