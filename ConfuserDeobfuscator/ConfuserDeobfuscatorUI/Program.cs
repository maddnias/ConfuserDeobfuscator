using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator;
using ConfuserDeobfuscator.Deobfuscators;

namespace ConfuserDeobfuscatorUI
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = new Deobfuscator19R();
            d.ProcessFile(args[0]);

            Console.ReadLine();
        }
    }
}
