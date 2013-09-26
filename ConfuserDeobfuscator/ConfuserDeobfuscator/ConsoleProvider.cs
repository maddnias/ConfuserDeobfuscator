using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator
{
    public class ConsoleProvider : IUserInterfaceProvider
    {
        public void WriteVerbose(string data, int node, bool nl = true)
        {
            if (Ctx.LoggingLevel != Ctx.OutputLevel.Verbose) 
                return;
            for (var i = 0; i < node; i++)
                data = data.Insert(0, "-");
            if (node > 0)
                data = data.Insert(node, " ");

            if (nl)
                Console.WriteLine(data);
            else
                Console.Write(data);
        }

        public void WriteVerbose(string formattedData, int node, bool nl = true, params object[] data)
        {
            if (Ctx.LoggingLevel != Ctx.OutputLevel.Verbose) 
                return;
            for (var i = 0; i < node; i++)
                formattedData = formattedData.Insert(0, "-");
            if (node > 0)
                formattedData = formattedData.Insert(node, " ");

            if (nl)
                Console.WriteLine(formattedData, data);
            else
                Console.Write(formattedData, data);
        }

        public void Write(string data, int node, bool nl = true)
        {
            if (Ctx.LoggingLevel == Ctx.OutputLevel.None)
                return;
            for (var i = 0; i < node; i++)
                data = data.Insert(0, "-");
            if (node > 0)
                data = data.Insert(node, " ");

            if (nl)
                Console.WriteLine(data);
            else
                Console.Write(data);
        }

        public void Write(string formattedData, int node, bool nl = true, params object[] data)
        {
            if (Ctx.LoggingLevel == Ctx.OutputLevel.None)
                return;
            for (var i = 0; i < node; i++)
                formattedData = formattedData.Insert(0, "-");
            if (node > 0)
                formattedData = formattedData.Insert(node, " ");

            if (nl)
                Console.WriteLine(formattedData, data);
            else
                Console.Write(formattedData, data);
        }
    }
}
