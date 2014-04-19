using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Bea;
using ConfuserDeobfuscator.Engine.Routines.Ex.x86.Instructions;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Routines.Ex.x86
{
    public sealed class X86Method
    {
        public List<X86Instruction> Instructions;

        public Stack<int> LocalStack = new Stack<int>();
        public Dictionary<string, int> Registers = new Dictionary<string, int>
        {
            {"EAX", 0},
            {"EBX", 0},
            {"ECX", 0},
            {"EDX", 0},
            {"ESP", 0},
            {"EBP", 0},
            {"ESI", 0},
            {"EDI", 0}
        };


        public X86Method(MethodDef method)
        {
            Instructions = new List<X86Instruction>();
            ParseInstructions(method);
        }

        private void ParseInstructions(MethodDef method)
        {
            var rawInstructions = new List<Disasm>();
            var body = method.ReadBodyFromRva();
            var disasm = new Disasm();
            var buff = body.ToUnmanagedBuffer();

            disasm.EIP = new IntPtr(buff.Ptr.ToInt32());

            for (var i = 0; i < body.Length; i++)
            {
                var res = BeaEngine.Disasm(disasm);

                if (disasm.Instruction.Mnemonic == "ret ")
                {
                    rawInstructions.Add(disasm.Clone());
                    break;
                }

                rawInstructions.Add(disasm.Clone());
                disasm.EIP = new IntPtr(disasm.EIP.ToInt32() + res);
            }

            Marshal.FreeHGlobal(buff.Ptr);

            foreach (var instr in rawInstructions)
            {
                switch (instr.Instruction.Mnemonic.Trim())
                {
                    case "mov":
                        Instructions.Add(new X86MOV(instr));
                        break;
                    case "add":
                        Instructions.Add(new X86ADD(instr));
                        break;
                    case "sub":
                        Instructions.Add(new X86SUB(instr));
                        break;
                    case "imul":
                        Instructions.Add(new X86IMUL(instr));
                        break;
                    case "div":
                        Instructions.Add(new X86DIV(instr));
                        break;
                    case "neg":
                        Instructions.Add(new X86NEG(instr));
                        break;
                    case "not":
                        Instructions.Add(new X86NOT(instr));
                        break;
                    case "xor":
                        Instructions.Add(new X86XOR(instr));
                        break;
                    case "pop":
                        Instructions.Add(new X86POP(instr));
                        break;
                }
            }
        }

        public int Execute(params int[] @params)
        {
            foreach (var param in @params)
                LocalStack.Push(param);

            foreach (var instr in Instructions)
                instr.Execute(Registers, LocalStack);

            return Registers["EAX"];
        }
    }
}
