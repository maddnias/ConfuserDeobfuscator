using System.Collections.Generic;
using ConfuserDeobfuscator.Engine.Base;

namespace ConfuserDeobfuscator.Engine.Routines.Base
{
    public abstract class DeobfuscationRoutine18 : IDeobfuscationRoutine
    {
        public abstract Dictionary<string, object> RoutineVariables { get; set; }
        public abstract string Title { get; }
        public abstract bool Detect();
        public abstract void Initialize();
        public abstract void Process();
        public abstract void CleanUp();
    }
}
