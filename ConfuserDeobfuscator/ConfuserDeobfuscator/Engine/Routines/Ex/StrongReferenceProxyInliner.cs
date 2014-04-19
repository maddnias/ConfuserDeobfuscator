using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator.Engine.Routines.Base;

namespace ConfuserDeobfuscator.Engine.Routines.Ex
{
    class StrongReferenceProxyInliner : DeobfuscationRoutineEx
    {
        public override string Title
        {
            get { return "Resolving reference proxies"; }
        }

        public override bool Detect()
        {
            return false;
        }

        public override void Initialize()
        {

        }

        public override void Process()
        {

        }

        public override void CleanUp()
        {
          
        }
    }
}
