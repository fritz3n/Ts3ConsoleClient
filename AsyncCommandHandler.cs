using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using TS3Client.Messages;
using TS3Client.Full;

namespace TS3Client
{
    public static class AsyncComHandler
    {
        static public object Handler;
        static private bool running = false;
        static private List<MethodDescriptor> Methods = new List<MethodDescriptor>();


        public static void BeginHandler(object HandlingObject)
        {
            if (running)
            {

                throw new Exception("Already running!");
                return;
            }

            running = true;

            Handler = HandlingObject;
            

            if (Handler != null)
            {
                MethodInfo[] arr = Handler.GetType().GetMethods();

                foreach (MethodInfo inf in arr)
                {
                    try
                    {
                        Methods.Add(new MethodDescriptor(inf));
                    }
                    catch { }
                }

                new Task(() => { ConsoleWorker(); }).Start();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(">");
            }
            else
            {
                throw new NullReferenceException("HandlingObject Handler is null!");
            }
        }
        
        private static void ConsoleWorker()
        {
            while (true)
            {
                string s = Console.ReadLine();

                HandleCommand(s);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(">");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void execute(string Data)
        {
            string[] Lines = Data.Split(new[] { '\r', '\n' });

            Console.WriteLine();

            foreach(string Line in Lines)
            {
                Console.WriteLine("->" + Line);
                HandleCommand(Line);
            }

        }

        public static void execute(string[] Lines)
        {
            Console.WriteLine();

            foreach (string Line in Lines)
            {
                Console.WriteLine("->" + Line);
                HandleCommand(Line);
            }

        }

        public static void HandleCommand(string Line, Context context = null)
        {
            if(context == null)
                context = new Context();
            

            string[] rawArray = Line.Split(new char[] { ' ' }, 2);
            string command = rawArray[0].ToLower();

            List<string> args = new List<string>();

            if (rawArray.Length > 1)
            {

                string pattern = "[\"'](.*?[^\\\\])[\"']|([^ \"'\\s]+)";

                Regex rgx = new Regex(pattern);
                int[] groupNumbers = rgx.GetGroupNumbers();

                Match m = rgx.Match(rawArray[1]);
                while (m.Success)
                {
                    for (int i = 1; i <= 2; i++)
                    {
                        Group g = m.Groups[i];
                        CaptureCollection cc = g.Captures;
                        if (cc.Count > 0)
                        {
                            args.Add(cc[0].Value);
                        }
                    }
                    m = m.NextMatch();
                }
            }

            if(command == "help")
            {
                if(args.Count > 0)
                {
                    GetHelp(args[0], context);
                }
                else
                {
                    GetHelp(null, context);
                }
                return;
            }

            bool MatchingName = false;
            bool MatchingParams = false;

            foreach (MethodDescriptor Desc in Methods)
            {
                if (Desc.name.ToLower() == command | Desc.Alias.Contains<string>(command))
                {
                    MatchingName = true;

                    if (Desc.TryInvoke(args, Handler, context))
                    {
                        MatchingParams = true;
                        break;
                    }
                }
            }

            if (!MatchingName)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                context.WriteLine("No such command found!\nUse Help to list all commands!");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (!MatchingParams)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                context.WriteLine("No command with such arguments found!");
                GetHelp(command,context);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static private void GetHelp(string Name = null, Context context = null)
        {
            if (context == null)
                context = new Context();

            context.BufferOn();
            context.WriteLine();
            foreach (MethodDescriptor Desc in Methods)
            {
                if (Name == null || Desc.name.ToLower() == Name.ToLower() | Desc.Alias.Contains(Name.ToLower()))
                {
                    Restriction rest = Desc.Info.GetCustomAttribute<Restriction>();
                    if (rest != null && rest.ts == false)
                        context.Write("[No Ts]");

                    context.Write(Desc.name + "(");
                    for(int i = 0; i < Desc.Params.Length; i++)
                    {
                        ParameterInfo PInfo = Desc.Params[i];

                        if (PInfo.GetCustomAttribute(typeof(Consume)) != null)
                            context.Write("[greedy] ");

                        context.Write(PInfo.ParameterType.Name + " " + PInfo.Name);

                        if (PInfo.HasDefaultValue)
                            context.Write(" = \"" + (PInfo.DefaultValue != null ? PInfo.DefaultValue.ToString() : "NULL") + "\"");
                        if (i != Desc.Params.Length - 1)
                            context.Write(", ");
                    }
                    context.WriteLine(")");
                    context.WriteLine("-" + Desc.Description);

                    if(Desc.Alias.Length != 0)
                    {
                        context.Write("Alias:");
                        foreach(string Alias in Desc.Alias)
                        {
                            context.Write(" " + Alias);
                        }
                        context.WriteLine();
                    }

                    if (Name == null)
                        context.WriteLine();
                }
            }
            context.Flush();
        }




    }

    public class GenericMethodException : Exception { }

    public class MethodDescriptor
    {
        public MethodInfo Info;
        public ParameterInfo[] Params;
        public int MinParams = 0;
        public int MaxParams = 0;
        public string name;
        public string Description;
        private List<string> GenericMethods = new List<string> {"ToString","GetType","GetHashCode","Equals"};
        public string[] Alias;

        public MethodDescriptor(MethodInfo Inf)
        {
            Info = Inf;

            Params = Info.GetParameters();

            name = Info.Name;

            if (GenericMethods.Contains(name))
            {
                throw new GenericMethodException();
            }

            object[] displayAttributes = Info.GetCustomAttributes<Desc>().ToArray<object>();
            if (displayAttributes != null && displayAttributes.Length != 0)
            {
                try
                {
                    Description = ((Desc)displayAttributes[0]).Description;
                }
                catch { }
            }
            else
            {
                Description = "No Description!";
            }

            Alias aliasAttribute = Info.GetCustomAttribute<Alias>();

            if (aliasAttribute != null)
                Alias = aliasAttribute.alias;
            else
                Alias = new string[0];
            
            MaxParams = Params.Length;

            foreach(ParameterInfo ParamInfo in Params)
            {
                if (!ParamInfo.HasDefaultValue)
                {
                    MinParams++;
                }

                if (ParamInfo.GetCustomAttribute(typeof(Consume)) != null)
                {
                    MaxParams = Int32.MaxValue;
                }
            }
        }

        public bool TryInvoke(List<string> Arguments, object Handler, Context context)
        {
            if(Arguments.Count < MinParams | Arguments.Count > MaxParams)
            {
                return false;
            }

            if (!CheckRestriction(context))
            {
                return false;
            }

            object[] ParsedAguments = new object[Params.Length];

            for (int i = 0; i < Params.Length; i++)
            {
                if (Arguments.Count > i)
                {
                    string Argument = "";
                    try
                    {
                        if (Params[i].GetCustomAttribute(typeof(Consume)) != null)
                        {
                            for (int j = i; j < Arguments.Count; j++)
                            {
                                Argument += Arguments[j];

                                if (j != Arguments.Count - 1)
                                {
                                    Argument += " ";
                                }
                            }
                            Arguments.Clear();
                        }
                        else
                        {
                            Argument = Arguments[i];
                        }
                    }catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    object ParsedArgument;

                    try
                    {
                        ParsedArgument = TypeDescriptor.GetConverter(Params[i].ParameterType).ConvertFromString(Argument);
                    }
                    catch
                    {
                        return false;
                    }

                    ParsedAguments[i] = ParsedArgument;
                }
                else
                {
                    ParsedAguments[i] = Params[i].DefaultValue;
                }
            }
            object Return = null;

            ((CommandHandler)Handler).context = context;

            try
            {
                Return = Info.Invoke(Handler, ParsedAguments);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (Return != null)
            {
                context.WriteLine(Return);
            }

            return true;
        }

        private bool CheckRestriction(Context context)
        {
            Restriction Rest = Info.GetCustomAttribute<Restriction>();

            if (Rest == null)
                return true;

            if (context.useTs & Rest.ts)
                return true;
            if (!context.useTs & Rest.console)
                return true;

            return false;
        }

    }

    public class Desc : Attribute
    {
        public string Description;

        public Desc(string desc)
        {
            Description = desc;
        }
    }

    public class Alias : Attribute
    {
        public string[] alias;

        public Alias(params string[] Alias)
        {
            alias = Alias;
        }
    }

    public class Consume : Attribute
    {
    }

    public class Restriction : Attribute
    {
        public bool console = true;
        public bool ts = true;

        public Restriction(bool Ts = true, bool Console = true)
        {
            console = Console;
            ts = Ts;
        }
    }

}
