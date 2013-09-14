using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator;

namespace ConfuserDeobfuscatorUI
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = new Deobfuscator();
            d.ProcessFile(args[0]);

            Console.ReadLine();
        }
    }
}
