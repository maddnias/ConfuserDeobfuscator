using System.Collections.Generic;

namespace ConfuserDeobfuscator.Engine.Base
{
    public interface IDeobfuscationRoutine
    {
        //AssemblyDef Assembly { get; set; }
        //IUserInterfaceProvider UIProvider { get; set; }
        Dictionary<string, object> RoutineVariables { get; set; }
        string Title { get; }

        bool Detect();
        void Initialize();
        void Process();
        void CleanUp();
    }
}
