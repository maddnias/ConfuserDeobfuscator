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
    class X86DIV : X86Instruction
    {
        public X86DIV(Disasm rawInstruction) : base()
        {
            Operands = new IX86Operand[2];
            Operands[0] = rawInstruction.Argument1.GetOperand();
            Operands[1] = rawInstruction.Argument2.GetOperand();
        }

        public override X86OpCode OpCode { get { return X86OpCode.DIV; } }

        public override void Execute(Dictionary<string, int> registers, Stack<int> localStack)
        {
            
        }
    }
}
