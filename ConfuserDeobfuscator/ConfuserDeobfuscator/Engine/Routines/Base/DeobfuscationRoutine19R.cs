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
    public abstract class DeobfuscationRoutine19R : IDeobfuscationRoutine, IConstantDemutator, IDelegateResolver
    {
        public Dictionary<string, object> RoutineVariables { get; set; }
        public Dictionary<string, DemutatedKeys> DemutatedKeys { get; set;}
        public abstract string Title { get; }

        protected DeobfuscationRoutine19R()
        {
            RoutineVariables = new Dictionary<string, object>();
            DemutatedKeys = new Dictionary<string, DemutatedKeys>();
        }

        public abstract bool Detect();
        public abstract void Initialize();
        public abstract void Process();
        public abstract void CleanUp();

        public int[] DemutatedInts { get; set; }
        public long[] DemutatedLongs { get; set; }

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
                                      //(x => x.OpCode.Code == Code.Mul),
                                      (x => x.OpCode.Code == Code.Ldc_I8),
                                  };
            var stackvals = new List<dynamic>();
            var updatedValues = new List<Tuple<Instruction, Instruction[], Instruction>>();
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instr = method.Body.Instructions[i];
                Instruction ender;
                int size;
                if (instr.IsLdcI4() || instr.OpCode.Code == Code.Ldc_I8)
                    if (instr.FollowsPattern(method.Body, out ender, pattern, 2, out size))
                    {
                        var st = new ILEmulator(method.Body, method.Body.Instructions.Count);
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
            }
            foreach (var @new in updatedValues)
            {
                method.Body.SimplifyBranches();
                var initIdx = @new.Item1.GetInstructionIndex(method.Body.Instructions);
                if (@new.Item1.IsLdcI4())
                {
                    if (@new.Item1.GetLdcI4Value() == @new.Item3.GetLdcI4Value())
                        continue;
                }
                else
                    if ((dynamic)@new.Item1.Operand == (dynamic)@new.Item3.Operand)
                        continue;
                foreach (var instr in @new.Item2.Where(instr => instr != null))
                    method.Body.Instructions.Remove(instr);
                method.Body.Instructions.Insert(initIdx, @new.Item3);
                method.Body.Instructions.Remove(@new.Item1);
                method.Body.OptimizeBranches();
            }
        }

        //public IEnumerable<IMemberRef> ResolveProxyMethod(TypeDef proxy, int key)
        //{
        //    foreach (var fld in proxy.Fields)
        //    {
        //        var fldSig = DeobfuscatorContext.OriginalMD.ReadBlob(fld.MDToken.ToUInt32());
        //        var x = ((uint)fldSig[fldSig.Length - 6] << 0) |
        //                ((uint)fldSig[fldSig.Length - 5] << 8) |
        //                ((uint)fldSig[fldSig.Length - 3] << 16) |
        //                ((uint)fldSig[fldSig.Length - 2] << 24);

        //        var method = DeobfuscatorContext.OriginalMD.ResolveMemberRef(MDToken.ToRID((uint)(x ^ key | (fldSig[fldSig.Length - 7] << 24))));
        //        yield return method;
        //    }
        //}

        public IEnumerable<Tuple<IMemberRef, FieldDef>> ResolveProxyMethod(TypeDef proxy, int key)
        {
            foreach (var fld in proxy.Fields)
            {
                var fldSig = DeobfuscatorContext.OriginalMD.ReadBlob(fld.MDToken.ToUInt32());
                var x = ((uint)fldSig[fldSig.Length - 6] << 0) |
                        ((uint)fldSig[fldSig.Length - 5] << 8) |
                        ((uint)fldSig[fldSig.Length - 3] << 16) |
                        ((uint)fldSig[fldSig.Length - 2] << 24);

                var method = DeobfuscatorContext.OriginalMD.ResolveMemberRef(MDToken.ToRID((uint)(x ^ key | (fldSig[fldSig.Length - 7] << 24))));
                yield return Tuple.Create(method as IMemberRef, fld);
            }
        }

        public int ReadKey(MethodDef proxyGenerator)
        {
            var initInstr =
                proxyGenerator.Body.Instructions.FindInstruction(
                    x =>x.OpCode.Code == Code.Or, 2);

            while (initInstr.Next(proxyGenerator.Body) != null && !initInstr.Next(proxyGenerator.Body).IsLdcI4())
                initInstr = initInstr.Next(proxyGenerator.Body);
            //return
            //    proxyGenerator.Body.Instructions.FindInstruction(x => x.OpCode.Code == Code.Callvirt, 3)
            //                  .Next(proxyGenerator.Body).Next(proxyGenerator.Body).GetLdcI4Value();
            return initInstr.Next(proxyGenerator.Body).GetLdcI4Value();
        }

        public virtual void RestoreProxyCall(Tuple<TypeDef, IMemberRef, FieldDef> resolvedProxy) { }
        public virtual void RestoreProxyCall(Tuple<TypeDef, IMemberRef, FieldDef> resolvedProxy, char virtIdentifier) { }
    }
}
