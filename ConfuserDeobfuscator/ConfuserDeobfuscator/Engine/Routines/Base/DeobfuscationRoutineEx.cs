using System;
using System.Collections.Generic;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Engine.Routines.Base
{
    public abstract class DeobfuscationRoutineEx : IDeobfuscationRoutine, IConstantDemutator
    {
        public Dictionary<string, object> RoutineVariables { get; set; }
        public Dictionary<string, DemutatedKeys> DemutatedKeys { get; set;}
        public List<Tuple<MethodDef, Instruction[]>> RemovedInstructions { get; set; }
        public abstract string Title { get; }

        protected DeobfuscationRoutineEx()
        {
            RoutineVariables = new Dictionary<string, object>();
            DemutatedKeys = new Dictionary<string, DemutatedKeys>();
            RemovedInstructions = new List<Tuple<MethodDef, Instruction[]>>();
        }

        public abstract bool Detect();
        public abstract void Initialize();
        public abstract void Process();
        public abstract void CleanUp();

        public void FinalizeCleanUp()
        {
            foreach (var val in RemovedInstructions)
                UpdateBranchReferences(val.Item1, val.Item2);
        }

        private static void UpdateBranchReferences(MethodDef method, IEnumerable<Instruction> removedInstructions)
        {
            CilBody body = method.Body;

            foreach (var instr in removedInstructions)
            {
                Instruction newTarget = body.Instructions.FirstOrDefault(x => x.Offset == instr.Offset);

                foreach (var eh in body.ExceptionHandlers)
                {
                    if (eh.HandlerEnd == instr)
                        eh.HandlerEnd = newTarget;
                    if (eh.TryEnd == instr)
                        eh.TryEnd = newTarget;
                }

                if (newTarget == null)
                    return;

                foreach (var @ref in instr.FindAllReferences(body))
                {
                    if ((@ref.Operand as Instruction) == instr)
                        @ref.Operand = newTarget;
                }

                foreach (var eh in body.ExceptionHandlers)
                {
                    if (eh.HandlerStart == instr)
                            eh.HandlerStart = newTarget;
                    if (eh.TryStart == instr)
                            eh.TryStart = newTarget;
                }
            }
        }

        public void CalculateMutations(MethodDef method)
        {
            if (method == null || method.Body == null)
                return;
            var pattern = new List<Predicate<Instruction>>
                                  {
                                      (x => x.IsLdcI4_2()),
                                      (x => x.OpCode.Code == Code.Add),
                                      (x => x.OpCode.Code == Code.Xor),
                                      (x => x.OpCode.Code == Code.Sub),
                                      (x => x.OpCode.Code == Code.Neg),
                                      (x => x.OpCode.Code == Code.Mul),
                                      (x => x.OpCode.Code == Code.Ldc_I8),
                                  };
            var stackvals = new List<dynamic>();
            var updatedValues = new List<Tuple<Instruction, Instruction[], Instruction>>();
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                Instruction ender;
                int size;

                if (!instr.IsLdcI4() && instr.OpCode.Code != Code.Ldc_I8) continue;
                if (!instr.FollowsPattern(method.Body, out ender, pattern, 2, out size)) continue;

                var st = new ILEmulator(method.Body, size);
                try
                {
                    st.EmulateUntil(ender.Next(method.Body), method.Body, instr);
                    var fakedVal = false;
                    if (st.Stack.Count > 1)
                    {
                        if (instr.Previous(method.Body).OpCode.StackBehaviourPush != StackBehaviour.Push1)
                        {
                            fakedVal = true;
                            stackvals.Add((dynamic)st.Stack.Reverse().ToList()[0].Value);
                        }
                        else
                            continue;
                    }
                    else
                        stackvals.Add((dynamic) st.Stack.Peek().Value);
                    var oldInstr = instr.Next(method.Body);
                    var badInstrs = new Instruction[size - 1];
                    for (int j = i, x = 0; j < size + i - 1; j++, x++)
                    {
                        badInstrs[x] = oldInstr;
                        oldInstr = oldInstr.Next(method.Body);
                    }
                    if (fakedVal)
                    {
                        dynamic val = st.Stack.ToList().Last().Value;
                        if (val is int)
                            updatedValues.Add(Tuple.Create(instr, badInstrs,
                                Instruction.CreateLdcI4(val)));
                        else
                            updatedValues.Add(Tuple.Create(instr, badInstrs,
                                Instruction.Create(OpCodes.Ldc_I8, (long)val)));
                        badInstrs[badInstrs.Length - 1] = null;
                    }
                    else
                    {
                        dynamic val = st.Stack.ToList().Last().Value;
                        if (val is int)
                            updatedValues.Add(Tuple.Create(instr, badInstrs,
                                Instruction.CreateLdcI4(val)));
                        else if(val != null)
                            updatedValues.Add(Tuple.Create(instr, badInstrs,
                                Instruction.Create(OpCodes.Ldc_I8, (long)val)));
                    }
                    i += size;
                }
                catch (InvalidOperationException)
                {
                }
            }
            foreach (var @new in updatedValues)
            {
                method.Body.SimplifyBranches();

                var initIdx = @new.Item1.GetInstructionIndex(method.Body.Instructions);
                if (@new.Item1.IsLdcI4()){
                    if (@new.Item1.GetLdcI4Value() == @new.Item3.GetLdcI4Value())
                        continue;
                }
                else
                    if ((dynamic)@new.Item1.Operand == (dynamic)@new.Item3.Operand)
                        continue;

                var removedInstructions = new Instruction[@new.Item2.Count(x => x != null) +1];
                var idx = 0;
                foreach (var instr in @new.Item2.Where(instr => instr != null))
                {
                    removedInstructions[idx++] = instr;
                    method.Body.Instructions.Remove(instr);
                }

                method.Body.Instructions.Insert(initIdx, @new.Item3);
                method.Body.Instructions.Remove(@new.Item1);
                method.Body.OptimizeBranches();

                removedInstructions[removedInstructions.Length - 1] = @new.Item1;
                RemovedInstructions.Add(Tuple.Create(method, removedInstructions));
            }
        }
    }
}
