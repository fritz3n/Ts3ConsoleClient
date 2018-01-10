using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TS3Client.Full;

namespace TS3Client
{
    // Token: 0x02000005 RID: 5
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleHelper.Start();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            CommandHandler Handler = new CommandHandler();
            
            AsyncComHandler.BeginHandler(Handler);
           
            Thread.Sleep(Timeout.Infinite);
            
        }
    }
}
