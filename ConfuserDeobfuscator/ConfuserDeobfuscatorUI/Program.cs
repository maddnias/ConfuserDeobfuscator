using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfuserDeobfuscator;
using ConfuserDeobfuscator.Deobfuscators;

namespace ConfuserDeobfuscatorUI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                var d = DeobfuscatorFactory.CreateDeobfuscator(args[0]);
                d.ProcessFile(args[0]);
            }
            else
            {
                Console.WriteLine("No file Specified! \r\nPress any key to Exit");
            }

            Console.ReadLine();
        }
    }
}
