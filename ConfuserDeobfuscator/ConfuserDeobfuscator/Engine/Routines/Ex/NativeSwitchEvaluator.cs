using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Bea;
using ConfuserDeobfuscator.Engine.Routines.Base;
using ConfuserDeobfuscator.Engine.Routines.Ex.x86;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines.Ex
{
    class NativeSwitchEvaluator : DeobfuscationRoutineEx
    {
        class NativeSwitch
        {
            public Instruction SwitchInstruction { get; set;}
            public Instruction NativeCall { get; set; }
            public MethodDef Method { get; set; }
            public MethodDef NativeMethod { get; set; }
            public int Parameter { get; set; }
            public Blocks Blocks { get; set; }
        }

        public override string Title
        {
            get { return "Evaluate value of native switches"; }
        }

        public override bool Detect()
        {
            var switches = new List<NativeSwitch>();

            foreach (var method in Ctx.Assembly.FindMethods(m => m.HasBody))
            {
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode.Code != Code.Switch)
                        continue;
                    if (!instr.Previous(method.Body).IsCall())
                        continue;

                    var call = instr.Previous(method.Body);
                    if (!IsNativeCall(call))
                        continue;

                    var emu = new ILEmulator(method.Body, method.Body.Instructions.Count);
                    var @params = emu.TraceCallParameters(call);

                    var instructions = @params as List<Instruction> ?? @params.ToList();
                    if (instructions.Count() != 2 || !instructions[0].IsLdcI4())
                        continue;

                    switches.Add(new NativeSwitch
                    {
                        Method = method,
                        NativeCall = call,
                        SwitchInstruction = instr,
                        NativeMethod = call.Operand as MethodDef,
                        Parameter = instructions[0].GetLdcI4Value(),
                        Blocks = new Blocks(method)
                    });
                }
            }

            RoutineVariables.Add("switches", switches);

            return switches.Count > 0;
        }

        public override void Initialize()
        {

        }

        public override void Process()
        {
            var switches = RoutineVariables["switches"] as List<NativeSwitch>;

            foreach (var @switch in switches)
            {
                var switchBlocks = new List<Block>();

                foreach (var block in @switch.Blocks.MethodBlocks.getAllBlocks())
                {
                    if (block.Sources.Count < 1)
                        continue;
                    if (block.Sources.Count(b => b.LastInstr.Instruction == @switch.SwitchInstruction) == 0)
                        continue;
                    if (!block.LastInstr.isLdcI4())
                        continue;

                    switchBlocks.Add(block);
                }

                var emuMethod = new X86Method(@switch.NativeMethod);
                var val = emuMethod.Execute(@switch.Parameter);
                var cleanSwitch = new List<Instruction>();

                for (var i = 0; i < switchBlocks.Count; i++)
                {
                    var curBlock =
                        switchBlocks.FirstOrDefault(
                            b => b.FirstInstr.Instruction == (@switch.SwitchInstruction.Operand as Instruction[])[val]);

                    if (curBlock == null)
                        break;

                    cleanSwitch.AddRange(curBlock.Instructions.ConvertAll(ii => ii.Instruction));
                    cleanSwitch.Remove(cleanSwitch.Last());
                    val = emuMethod.Execute(curBlock.LastInstr.getLdcI4Value());
                }

                var start = (@switch.SwitchInstruction.Operand as Instruction[]).ToList().Min(x => x.Offset);
                var end = (@switch.SwitchInstruction.Operand as Instruction[]).ToList().Max(x => x.Offset);

                var startIdx =
                    @switch.Method.Body.Instructions.First(x => x.Offset == start)
                        .GetInstructionIndex(@switch.Method.Body.Instructions);
                var endIdx = @switch.Method.Body.Instructions.First(x => x.Offset == end)
                    .GetInstructionIndex(@switch.Method.Body.Instructions);

                for (var i = startIdx; i < cleanSwitch.Count; i++)
                {
                    @switch.Method.Body.Instructions[i] = cleanSwitch[i - startIdx];
                }

                Console.WriteLine(startIdx + endIdx);
            }
        }

        public override void CleanUp()
        {

        }

        private bool IsNativeCall(Instruction call)
        {
            var target = call.Operand as MethodDef;

            if (target == null)
                return false;
            if (target.DeclaringType.Name != "<Module>" || target.HasBody)
                return false;
            if (!target.IsPreserveSig)
                return false;
            if (!target.ImplAttributes.HasFlag(MethodImplAttributes.Unmanaged))
                return false;
            return target.CodeType == MethodImplAttributes.Native;
        }
    }
}
