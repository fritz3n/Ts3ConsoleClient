using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS3Client
{
    static class ConsoleHelper
    {
        static private Process p;
        static private StreamWriter sw;
        static private StreamReader sr;
        static private EventDisplay form;
        static private Task formTask;

        public static void Start()
        {
            form = new EventDisplay();

            formTask = new Task(() => { Application.Run(form); });
            formTask.Start();
        }

        delegate void WriteLine(string Str);

        public static void WriteEventLine(string Event)
        {
            if (form.InvokeRequired)
                form.Invoke(new WriteLine(WriteEventLine), new object[] { Event });
            else
                form.Write(Event + "\n");
            return;
        }

        delegate void Write(string Str);

        public static void WriteEvent(string Event)
        {
            if (form.InvokeRequired)
                form.Invoke(new Write(WriteEvent), new object[] { Event });
            else
                form.Write(Event);
            return;
        }

        delegate void WriteLineColor(string Str, Color col);

        public static void WriteEventLine(string Event, Color col)
        {
            if (form.InvokeRequired)
                form.Invoke(new WriteLineColor(WriteEventLine), new object[] { Event, col});
            else
                form.Write(Event + "\n",col);
            return;
        }

        delegate void WriteColor(string Str, Color col);

        public static void WriteEvent(string Event, Color col)
        {
            if (form.InvokeRequired)
                form.Invoke(new WriteColor(WriteEvent), new object[] { Event, col});
            else
                form.Write(Event, col);
            return;
        }
    }
}
