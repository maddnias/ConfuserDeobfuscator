using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace ConfuserDeobfuscator.Engine.Base
{
    interface IMetadataWorker
    {
        ModuleDefMD ModMD { get; set; }
    }
}
