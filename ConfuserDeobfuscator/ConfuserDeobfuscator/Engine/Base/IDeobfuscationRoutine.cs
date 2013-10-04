using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ConfuserDeobfuscator.Engine.Base
{
    public interface IDeobfuscationRoutine
    {
        //AssemblyDef Assembly { get; set; }
        //IUserInterfaceProvider UIProvider { get; set; }
        List<Tuple<MethodDef, Instruction[]>> RemovedInstructions { get; set; }
        Dictionary<string, object> RoutineVariables { get; set; }
        string Title { get; }

        bool Detect();
        void Initialize();
        void Process();
        void CleanUp();
        void FinalizeCleanUp();
    }
}
