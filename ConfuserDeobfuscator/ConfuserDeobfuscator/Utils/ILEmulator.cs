using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;
namespace ConfuserDeobfuscator.Utils
{
    public class ILEmulator
    {
        public class Snapshot
        {
            public Stack<StackEntry> ClonedStack;
            public Dictionary<int, LocalEntry> ClonedLocals;
            public Instruction CurrentInstruction;
            public CilBody Body;

            public Snapshot Previous;

            public Snapshot(Stack<StackEntry> clonedStack,
                Dictionary<int, LocalEntry> clonedLocals,
                Instruction currentInstruction,
                CilBody body,
                Snapshot previous)
            {
                ClonedStack = clonedStack;
                ClonedLocals = clonedLocals;
                CurrentInstruction = currentInstruction;
                Body = body;
                Previous = previous;
            }
        }

        public abstract class Entry
        {
            protected Entry(Instruction by, bool known, object value = null)
            {
                PushedBy = by;
                IsValueKnown = known;
                Value = value;
            }

            public Instruction PushedBy;
            public object Value;
            public bool IsValueKnown;
        }

        private CilBody _methodBody;

        #region Execution state

        private int _instructionPointer = 0, _maxExecuteCount = 0, _currentCount = 0;
        private Stack<StackEntry> _stack = new Stack<StackEntry>();
        private Dictionary<int, LocalEntry> _locals = new Dictionary<int, LocalEntry>();
        public List<Snapshot> Snapshots = new List<Snapshot>();

        #endregion

        public Stack<StackEntry> Stack { get { return _stack; } }

        public ILEmulator(CilBody methodBody, int maxExecuteCount)
        {
            _methodBody = methodBody;
            _maxExecuteCount = maxExecuteCount;
        }

        public void EmulateUntil(Instruction instruction)
        {
            Snapshots.Add(new Snapshot(new Stack<StackEntry>(Stack), new Dictionary<int, LocalEntry>(_locals),
                                       instruction, _methodBody, null));
            Trace(() => _methodBody.Instructions[_instructionPointer] != instruction);
        }

        public void EmulateUntil(Instruction instruction, CilBody body, Instruction starter)
        {
            Snapshots.Add(new Snapshot(new Stack<StackEntry>(Stack), new Dictionary<int, LocalEntry>(_locals),
                                       instruction, _methodBody, null));
            _instructionPointer = starter.GetInstructionIndex(body.Instructions) - 1;
            Trace(() => _methodBody.Instructions[_instructionPointer] != instruction);
        }

        public void EmulatePattern(List<OpCode> allowedOpcodes, int minSize)
        {
            var instruction = _methodBody.Instructions[_instructionPointer];

            Snapshots.Add(new Snapshot(new Stack<StackEntry>(Stack), new Dictionary<int, LocalEntry>(_locals),
                                      instruction, _methodBody, null));

            instruction = _methodBody.Instructions[_instructionPointer];

            while (!allowedOpcodes.Contains(instruction.OpCode))
            {
                _instructionPointer = ExecuteInstruction(instruction);
                instruction = _methodBody.Instructions[_instructionPointer];
            }

            _instructionPointer = ExecuteInstruction(instruction);
        }

        public IEnumerable<Instruction> TraceCallParameters(Instruction instruction)
        {
            EmulateUntil(instruction);

            var target = (instruction.Operand as MethodDef);
            var currentSnapshot = Snapshots[Snapshots.Count - 1];

            while (currentSnapshot.ClonedStack.Count > Stack.Count - target.Parameters.Count)
            {
                yield return currentSnapshot.CurrentInstruction;
                currentSnapshot = currentSnapshot.Previous;
            }

            yield return instruction;
        }

        private void Trace(Func<bool> continueCondition)
        {
            while (continueCondition())
            {
                if (_instructionPointer == -1)
                    throw new Exception("Infinite loop?");

                var instruction = _methodBody.Instructions[_instructionPointer];
                _instructionPointer = ExecuteInstruction(instruction);
            }
        }

        private LocalEntry StackToLocalEntry(Instruction instruction, StackEntry stackEntry)
        {
            return new LocalEntry(instruction, stackEntry.IsValueKnown, stackEntry.Value);
        }

        private StackEntry LocalToStackEntry(Instruction instruction, LocalEntry localEntry)
        {
            return new StackEntry(instruction, localEntry.IsValueKnown, localEntry.Value);
        }

        private void SetLocalValue(int id, LocalEntry entry)
        {
            if (_locals.ContainsKey(id))
            {
                _locals[id] = entry;
            }
            else
            {
                _locals.Add(id, entry);
            }
        }

        private int ExecuteInstruction(Instruction instruction)
        {
            var wasExactInstructionProcessed = true;

            if (_currentCount >= _maxExecuteCount)
                return -1;

            #region big switch

            switch (instruction.OpCode.Code)
            {
                case Code.Nop:
                    break;
                case Code.Break:
                    break;
                case Code.Ldarg_0:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Ldarg_1:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Ldarg_2:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Ldarg_3:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Ldloc_0:
                    _stack.Push(LocalToStackEntry(instruction, _locals[0]));
                    break;
                case Code.Ldloc_1:
                    _stack.Push(LocalToStackEntry(instruction, _locals[1]));
                    break;
                case Code.Ldloc_2:
                    _stack.Push(LocalToStackEntry(instruction, _locals[2]));
                    break;
                case Code.Ldloc_3:
                    _stack.Push(LocalToStackEntry(instruction, _locals[3]));
                    break;
                case Code.Stloc_0:
                    SetLocalValue(0, StackToLocalEntry(instruction, _stack.Pop()));
                    break;
                case Code.Stloc_1:
                    SetLocalValue(1, StackToLocalEntry(instruction, _stack.Pop()));
                    break;
                case Code.Stloc_2:
                    SetLocalValue(2, StackToLocalEntry(instruction, _stack.Pop()));
                    break;
                case Code.Stloc_3:
                    SetLocalValue(3, StackToLocalEntry(instruction, _stack.Pop()));
                    break;
                case Code.Ldarg_S:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Ldarga_S:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Starg_S:
                    _stack.Pop();
                    break;
                case Code.Ldloc_S:
                    _stack.Push(LocalToStackEntry(instruction, _locals[0]));
                    break;
                case Code.Ldloca_S:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Stloc_S:
                    _stack.Pop();
                    break;
                case Code.Ldnull:
                    _stack.Push(new StackEntry(instruction, true, null));
                    break;
                case Code.Ldc_I4_M1:
                    _stack.Push(new StackEntry(instruction, true, (int)-1));
                    break;
                case Code.Ldc_I4_0:
                    _stack.Push(new StackEntry(instruction, true, (int)0));
                    break;
                case Code.Ldc_I4_1:
                    _stack.Push(new StackEntry(instruction, true, (int)1));
                    break;
                case Code.Ldc_I4_2:
                    _stack.Push(new StackEntry(instruction, true, (int)2));
                    break;
                case Code.Ldc_I4_3:
                    _stack.Push(new StackEntry(instruction, true, (int)3));
                    break;
                case Code.Ldc_I4_4:
                    _stack.Push(new StackEntry(instruction, true, (int)4));
                    break;
                case Code.Ldc_I4_5:
                    _stack.Push(new StackEntry(instruction, true, (int)5));
                    break;
                case Code.Ldc_I4_6:
                    _stack.Push(new StackEntry(instruction, true, (int)6));
                    break;
                case Code.Ldc_I4_7:
                    _stack.Push(new StackEntry(instruction, true, (int)7));
                    break;
                case Code.Ldc_I4_8:
                    _stack.Push(new StackEntry(instruction, true, (int)8));
                    break;
                case Code.Ldc_I4_S:
                case Code.Ldc_I4:
                    _stack.Push(new StackEntry(instruction, true, (int)Convert.ChangeType(instruction.Operand, typeof(int))));
                    break;
                case Code.Ldc_I8:
                    _stack.Push(new StackEntry(instruction, true, (long)instruction.Operand));
                    break;
                case Code.Ldc_R4:
                    _stack.Push(new StackEntry(instruction, true, instruction.Operand));
                    break;
                case Code.Ldc_R8:
                    _stack.Push(new StackEntry(instruction, true, instruction.Operand));
                    break;
                case Code.Dup:
                    var value = _stack.Pop();
                    _stack.Push(value);
                    value.PushedBy = instruction;
                    _stack.Push(value);
                    break;
                case Code.Pop:
                    _stack.Pop();
                    break;
                //TODO: Implement
                /*case Code.Jmp:
                    break;*/
                case Code.Call:
                    dynamic mr;

                    if (instruction.Operand is MethodSpec)
                        mr = (instruction.Operand as MethodSpec).Method;
                    else
                        mr = (instruction.Operand as MemberRef);

                    for (int i = 0; i < mr.Parameters.Count; i++)
                        _stack.Pop();

                    if ((instruction.Operand as MethodDef).ReturnType != mr.Module.ImportAsTypeSig(typeof(void)))
                    {
                        _stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Calli:
                    break;
                case Code.Ret:
                    break;
                case Code.Br:
                case Code.Br_S:
                    return _methodBody.Instructions.IndexOf(instruction.Operand as Instruction);
                case Code.Brfalse:
                case Code.Brfalse_S:
                    if (Stack.Peek().IsValueKnown && Stack.Peek().Value.GetType().CanCastTo<bool>(Stack.Peek().Value))
                        if (!Convert.ToBoolean(Stack.Pop().Value))
                            return _methodBody.Instructions.IndexOf(instruction.Operand as Instruction);
                    break;
                case Code.Brtrue:
                case Code.Brtrue_S:
                    if (Stack.Peek().IsValueKnown && Stack.Peek().Value.GetType().CanCastTo<bool>(Stack.Peek().Value))
                        if (Convert.ToBoolean(Stack.Pop().Value))
                            return _methodBody.Instructions.IndexOf(instruction.Operand as Instruction);
                    break;
                case Code.Add:
                    if (Stack.Count < 2)
                        Stack.Push(new StackEntry(null, false));
                    if (Stack.VerifyTop())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value + (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Sub:
                    if (Stack.Count < 2)
                        Stack.Push(new StackEntry(null, false));
                    if (Stack.VerifyTop())
                    {
                        var holder = Stack.Pop().Value;
                        Stack.Push(new StackEntry(instruction, true,
                                                  (dynamic)Stack.Pop().Value - (dynamic)holder));
                    }
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Mul:
                    if (Stack.Count < 2)
                        Stack.Push(new StackEntry(null, false));
                    if (Stack.VerifyTop())
                    {
                        var holder = Stack.Pop().Value;
                        Stack.Push(new StackEntry(instruction, true,
                                                  (dynamic)Stack.Pop().Value * (dynamic)holder));
                    }
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Div_Un:
                case Code.Div:
                    if (Stack.VerifyTop())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value / (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Rem:
                case Code.Rem_Un:
                    if (Stack.VerifyTop())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value % (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Xor:
                    if (Stack.Count < 2)
                        Stack.Push(new StackEntry(null, false));
                    if (Stack.VerifyTop())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value ^ (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.And:
                    if (Stack.VerifyTop<int>())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value & (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Or:
                    if (Stack.VerifyTop<int>())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value | (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Shl:
                    if (Stack.VerifyTop<int>())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value << (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Shr_Un:
                case Code.Shr:
                    if (Stack.VerifyTop<int>())
                        Stack.Push(new StackEntry(instruction, true, (dynamic)Stack.Pop().Value >> (dynamic)Stack.Pop().Value));
                    else
                    {
                        Stack.Pop();
                        Stack.Pop();
                        Stack.Push(new StackEntry(instruction, false));
                    }
                    break;
                case Code.Neg:
                    if (Stack.Peek().IsValueKnown)
                        if (Stack.Peek().Value.IsNumeric())
                        {
                            Stack.Push(new StackEntry(instruction, true, -((dynamic)Stack.Pop().Value)));
                        }
                    break;
                case Code.Ldstr:
                    Stack.Push(new StackEntry(instruction, true, instruction.Operand as string));
                    break;
                case Code.Conv_I8:
                    if (Stack.Peek().IsValueKnown)
                        if (Stack.Peek().Value is int)
                            Stack.Push(new StackEntry(instruction, true,
                                                      (long)Convert.ChangeType(Stack.Pop().Value, typeof(long))));
                    break;
                case Code.Conv_I1:
                    if (Stack.Peek().IsValueKnown)
                        if (Stack.Peek().Value.IsNumeric())
                            Stack.Push(new StackEntry(instruction, true, Convert.ToByte(Stack.Pop().Value)));
                    break;
                case Code.Conv_I2:
                    if (Stack.Peek().IsValueKnown)
                        if (Stack.Peek().Value.IsNumeric())
                            Stack.Push(new StackEntry(instruction, true, Convert.ToInt16(Stack.Pop().Value)));
                    break;
                case Code.Conv_I4:
                    if (Stack.Peek().IsValueKnown)
                        if (Stack.Peek().Value.IsNumeric())
                            Stack.Push(new StackEntry(instruction, true, Convert.ToInt32(Stack.Pop().Value)));
                    break;
                case Code.Conv_R4:
                    break;
                case Code.Conv_R8:
                    break;
                case Code.Conv_U4:
                    break;
                case Code.Conv_U8:
                    break;
                case Code.Beq_S:
                    if (Stack.VerifyTop<int>())
                    {
                        if (Stack != null && Stack.Pop().Value == Stack.Pop().Value)
                            return _methodBody.Instructions.IndexOf(instruction.Operand as Instruction);
                    }
                    else if (Stack.VerifyTop())
                        if (ReferenceEquals(Stack.Pop().Value, Stack.Pop().Value))
                            return _methodBody.Instructions.IndexOf(instruction.Operand as Instruction);
                    break;
                case Code.Bge:
                case Code.Bge_Un_S:
                case Code.Bge_S:

                    break;
                case Code.Bgt_S:
                    break;
                case Code.Ble_S:
                    break;
                case Code.Blt_S:
                    break;
                case Code.Bne_Un_S:
                    break;
                    break;
                case Code.Bgt_Un_S:
                    break;
                case Code.Ble_Un_S:
                    break;
                case Code.Blt_Un_S:
                    break;
                case Code.Beq:
                    break;
                case Code.Bgt:
                    break;
                case Code.Ble:
                    break;
                case Code.Blt:
                    break;
                case Code.Bne_Un:
                    break;
                case Code.Bge_Un:
                    break;
                case Code.Bgt_Un:
                    break;
                case Code.Ble_Un:
                    break;
                case Code.Blt_Un:
                    break;
                case Code.Switch:
                    break;
                case Code.Ldind_I1:
                    break;
                case Code.Ldind_U1:
                    break;
                case Code.Ldind_I2:
                    break;
                case Code.Ldind_U2:
                    break;
                case Code.Ldind_I4:
                    break;
                case Code.Ldind_U4:
                    break;
                case Code.Ldind_I8:
                    break;
                case Code.Ldind_I:
                    break;
                case Code.Ldind_R4:
                    break;
                case Code.Ldind_R8:
                    break;
                case Code.Ldind_Ref:
                    break;
                case Code.Stind_Ref:
                    break;
                case Code.Stind_I1:
                    break;
                case Code.Stind_I2:
                    break;
                case Code.Stind_I4:
                    break;
                case Code.Stind_I8:
                    break;
                case Code.Stind_R4:
                    break;
                case Code.Stind_R8:
                    break;
                case Code.Not:
                    break;
                case Code.Callvirt:
                    break;
                case Code.Cpobj:
                    break;
                case Code.Ldobj:
                    break;
                case Code.Newobj:
                    _stack.Push(new StackEntry(instruction, false));
                    break;
                case Code.Castclass:
                    break;
                case Code.Isinst:
                    break;
                case Code.Conv_R_Un:
                    break;
                case Code.Unbox:
                    break;
                case Code.Throw:
                    break;
                case Code.Ldfld:
                    break;
                case Code.Ldflda:
                    break;
                case Code.Stfld:
                    break;
                case Code.Ldsfld:
                    break;
                case Code.Ldsflda:
                    break;
                case Code.Stsfld:
                    break;
                case Code.Stobj:
                    break;
                case Code.Conv_Ovf_I1_Un:
                    break;
                case Code.Conv_Ovf_I2_Un:
                    break;
                case Code.Conv_Ovf_I4_Un:
                    break;
                case Code.Conv_Ovf_I8_Un:
                    break;
                case Code.Conv_Ovf_U1_Un:
                    break;
                case Code.Conv_Ovf_U2_Un:
                    break;
                case Code.Conv_Ovf_U4_Un:
                    break;
                case Code.Conv_Ovf_U8_Un:
                    break;
                case Code.Conv_Ovf_I_Un:
                    break;
                case Code.Conv_Ovf_U_Un:
                    break;
                case Code.Box:
                    break;
                case Code.Newarr:
                    break;
                case Code.Ldlen:
                    break;
                case Code.Ldelema:
                    break;
                case Code.Ldelem_I1:
                    break;
                case Code.Ldelem_U1:
                    break;
                case Code.Ldelem_I2:
                    break;
                case Code.Ldelem_U2:
                    break;
                case Code.Ldelem_I4:
                    break;
                case Code.Ldelem_U4:
                    break;
                case Code.Ldelem_I8:
                    break;
                case Code.Ldelem_I:
                    break;
                case Code.Ldelem_R4:
                    break;
                case Code.Ldelem_R8:
                    break;
                case Code.Ldelem_Ref:
                    break;
                case Code.Stelem_I:
                    break;
                case Code.Stelem_I1:
                    break;
                case Code.Stelem_I2:
                    break;
                case Code.Stelem_I4:
                    break;
                case Code.Stelem_I8:
                    break;
                case Code.Stelem_R4:
                    break;
                case Code.Stelem_R8:
                    break;
                case Code.Stelem_Ref:
                    break;
                case Code.Unbox_Any:
                    break;
                case Code.Conv_Ovf_I1:
                    break;
                case Code.Conv_Ovf_U1:
                    break;
                case Code.Conv_Ovf_I2:
                    break;
                case Code.Conv_Ovf_U2:
                    break;
                case Code.Conv_Ovf_I4:
                    break;
                case Code.Conv_Ovf_U4:
                    break;
                case Code.Conv_Ovf_I8:
                    break;
                case Code.Conv_Ovf_U8:
                    break;
                case Code.Refanyval:
                    break;
                case Code.Ckfinite:
                    break;
                case Code.Mkrefany:
                    break;
                case Code.Ldtoken:
                    break;
                case Code.Conv_U2:
                    break;
                case Code.Conv_U1:
                    break;
                case Code.Conv_I:
                    break;
                case Code.Conv_Ovf_I:
                    break;
                case Code.Conv_Ovf_U:
                    break;
                case Code.Add_Ovf:
                    break;
                case Code.Add_Ovf_Un:
                    break;
                case Code.Mul_Ovf:
                    break;
                case Code.Mul_Ovf_Un:
                    break;
                case Code.Sub_Ovf:
                    break;
                case Code.Sub_Ovf_Un:
                    break;
                case Code.Endfinally:
                    break;
                case Code.Leave:
                    break;
                case Code.Leave_S:
                    break;
                case Code.Stind_I:
                    break;
                case Code.Conv_U:
                    break;
                case Code.Arglist:
                    break;
                case Code.Ceq:
                    break;
                case Code.Cgt:
                    break;
                case Code.Cgt_Un:
                    break;
                case Code.Clt:
                    break;
                case Code.Clt_Un:
                    break;
                case Code.Ldftn:
                    break;
                case Code.Ldvirtftn:
                    break;
                case Code.Ldarg:
                    break;
                case Code.Ldarga:
                    break;
                case Code.Starg:
                    break;
                case Code.Ldloc:
                    break;
                case Code.Ldloca:
                    break;
                case Code.Stloc:
                    break;
                case Code.Localloc:
                    break;
                case Code.Endfilter:
                    break;
                case Code.Unaligned:
                    break;
                case Code.Volatile:
                    break;
                case Code.Tailcall:
                    break;
                case Code.Initobj:
                    break;
                case Code.Constrained:
                    break;
                case Code.Cpblk:
                    break;
                case Code.Initblk:
                    break;
                case Code.Rethrow:
                    break;
                case Code.Sizeof:
                    break;
                case Code.Refanytype:
                    break;
                case Code.Readonly:
                    break;
                default:
                    wasExactInstructionProcessed = false;
                    break;
            }

            #endregion

            _currentCount++;

            Snapshots.Add(new Snapshot(new Stack<StackEntry>(Stack), new Dictionary<int, LocalEntry>(_locals),
                                       instruction, _methodBody, Snapshots[Snapshots.Count - 1]));

            if (!wasExactInstructionProcessed)
            {
                switch (instruction.OpCode.StackBehaviourPush)
                {
                    case StackBehaviour.Push0:
                    case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                    case StackBehaviour.Pushi8:
                    case StackBehaviour.Pushr4:
                    case StackBehaviour.Pushr8:
                    case StackBehaviour.Pushref:
                        _stack.Push(new StackEntry(instruction, true, instruction.Operand));
                        break;
                    case StackBehaviour.Push1_push1:
                        _stack.Push(new StackEntry(instruction, false));
                        _stack.Push(new StackEntry(instruction, false));
                        break;
                }
                if (instruction.IsUnconditionalBranch())
                {
                    return _methodBody.Instructions.IndexOf(instruction);
                }
            }

            return ++_instructionPointer;
        }

        public class StackEntry : Entry
        {
            public StackEntry(Instruction @by, bool known, object value = null)
                : base(@by, known, value)
            {
            }
        }

        public class LocalEntry : Entry
        {
            public LocalEntry(Instruction @by, bool known, object value = null)
                : base(@by, known, value)
            {
            }
        }
    }
}
