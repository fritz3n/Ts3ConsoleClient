using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using AsyncCommandHandler;
using TS3Client.Full;
using TS3Client;
using TS3Client.Messages;
using TS3Client.Commands;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace TS3Client
{
    class CommandHandler
    {
        Ts3FullClient client;
        string id = "MG4DAgeAAgEgAiBza7WaNgvJvT4hmNQzDSmT1iuwHSa8RxEhgqjbfx174gIgSYAD+Z9Ecfq57RyHfQFNTl+1C24Uy0R57qj9Jd9KzX8CIQDvupoKOJk7jCeYYiMhvGOsKX1R38IRrRIschiwfA9dyQ==";

        public CommandHandler()
        {
            client = new Ts3FullClient(EventDispatchType.AutoThreadPooled);
            CommandHelper.client = client;
            Player.client = client;
            TsEventHandler.StartEventHandler(client);
        }
        [Desc("Change the clients name")]
        public void ChangeName(string name = "FaceBot")
        {
            if (String.IsNullOrEmpty(name))
            {
                return;
            }
            else
            {
                client.ChangeName(name);
            }
        }
        [Desc("Change the clients description")]
        public void ChangeDescription(string description = "FaceBot")
        {
            if (String.IsNullOrEmpty(description))
            {
                return;
            }
            else
            {
                client.ChangeDescription(description,client.ClientId);
            }
        }
        public void TestFN()
        {
            unchecked
            {
                string x = "" + (char)Int16.MaxValue;
                client.ChangeName(x);
            }
            
        }

        [Alias("c")]
        [Desc("Connect the bot")]
        public string Connect(string Name = "FaceBot", string Url = "c0d1n6.io",bool generateNewIdentity=true)
        {
            Console.WriteLine(Url);

            ConnectionDataFull Join = new ConnectionDataFull();

            Join.Address = "c0d1n6.io";
            if (generateNewIdentity==false) {
                Join.Identity = Ts3Crypt.LoadIdentity(id, ulong.Parse("187602236"));
            }
            else
            {
                Join.Identity = Ts3Crypt.GenerateNewIdentity();
            }

            Join.Username = Name;
            Join.VersionSign = VersionSign.VER_WIN_3_0_19_4;
            Join.HWID = "Cock 7";


            client.Connect(Join);

            Thread.Sleep(500);

            if (client.Connected)
                return "Ok";
            else
                return "Error!";
        }
        [Desc("List all available input sources...")]
        public void ListInputs()
        {
            int  c = WaveIn.DeviceCount;
            var x = WaveIn.GetCapabilities(0); 
            for(int i = 0; i < c; i++)
            {
                x = WaveIn.GetCapabilities(i);
                Console.WriteLine(i+"-"+x.ProductName);
            }
        }
        [Alias("dc")]
        [Desc("Disconnect the bot")]
        public string Disconnect()
        {
            if (!client.Connected)
                return "Not connected!";

            StopMic();
            StopVoice();

            client.Disconnect();
            return "Ok";
        }

        [Alias("close","bye","stop","ex")]
        [Desc("Exit the app")]
        public void Exit()
        {
            Disconnect();
            Environment.Exit(0);
        }

        [Desc("List clients")]
        public string List()
        {
            if (!client.Connected)
                return "Not connected!";
            CommandHelper.ResetColor();
            foreach (ClientData data in client.ClientList())
            {
                CommandHelper.AlternateColor();
                Console.WriteLine(data.ClientId + "\t" + data.NickName);
            }
            CommandHelper.ResetColor();

            return null;
        }

        [Desc("List Channels")]
        public string ListChannels()
        {
            if (!client.Connected)
                return "Not connected!";

            List<ResponseDictionary> Channels = client.SendCommand<ResponseDictionary>(new Ts3Command("channellist")).ToList();

            CommandHelper.ResetColor();
            foreach (ResponseDictionary Channel in Channels)
            {
                CommandHelper.AlternateColor();
                Console.WriteLine(" " + Channel["cid"] + "\t" + CommandHelper.Escape(Channel["channel_name"].Replace("\\s", " ")));
            }
            CommandHelper.ResetColor();

            return null;
        }

        [Desc("Join a channl")]
        public string Join(int ChannelId, string Password = null)
        {
            if (!client.Connected)
                return "Not connected!";

            client.ClientMove(client.ClientId, (ulong)ChannelId, Password);
            
            return "Ok";
        }

        [Alias("p")]
        [Desc("Poke someone")]
        public string Poke(int ClientId, [Consume] string Message = "test")
        {
            if (!client.Connected)
                return "Not connected!";

            
            client.Send("clientpoke", new ICommandPart[]
            {
                new CommandParameter("msg", Message),
                new CommandParameter("clid", ClientId)
            });
            

            return "OK";
        }

        [Desc("Poke everyone with the same Message")]
        public string PokeAll(string Message = "test")
        {
            if (!client.Connected)
                return "Not connected!";

            foreach (ClientData data in client.ClientList())
            {
                client.Send("clientpoke", new ICommandPart[]
                {
                        new CommandParameter("msg", Message),
                        new CommandParameter("clid", data.ClientId)
                });
            }

            return "Ok";
        }
        
        [Alias("m")]
        [Desc("Start Mic")]
        public string Mic(int DeviceNumber = 0)
        {
            if (!client.Connected)
                return "Not connected!";

            if (CommandHelper.mic)
            {
                CommandHelper.StopMic();
                return "Turned the Mic Off!";
            }

            CommandHelper.StartMic(DeviceNumber);

            Console.WriteLine("Now transmitting from " + WaveIn.GetCapabilities(DeviceNumber).ProductName);
            Console.WriteLine("Press c to close and stop the mic.");
            Console.WriteLine("Press k to close and keep the mic open.");

            while (true)
            {
                string Key = Console.ReadKey().Key.ToString().ToLower();
                Console.WriteLine();

                switch (Key)
                {
                    case "c":
                        CommandHelper.StopMic();
                        return "Ok";
                        break;

                    case "k":
                        return "Ok";
                        break;

                    default:
                        break;
                }
                
            }

            return "Ok";
        }

        [Alias("sm")]
        [Desc("Stop Mic")]
        public string StopMic()
        {
            if (!client.Connected)
                return "Not connected!";
            if (!CommandHelper.mic)
                return "Already Off!";

            CommandHelper.StopMic();
            return "Ok";
        }

        [Alias("v")]
        [Desc("Start voice output")]
        public string StartVoice(int Device = 0)
        {
            if (!client.Connected)
                return "Not connected!";
            if (client.PlayVoice)
                return "Already started!";

            try
            {
                Player.Start(Device);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "Ok";
        }

        [Alias("sv")]
        [Desc("Sop voice output")]
        public string StopVoice()
        {
            if (!client.Connected)
                return "Not connected!";
            if (!client.PlayVoice)
                return "Already stopped!";

            try
            {
                Player.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "Ok";
        }

        [Alias("pl")]
        [Desc("Sop voice output")]
        public string PlayWave(string Path, int Device = 0)
        {
            if (!client.Connected)
                return "Not connected!";
            try
            {
                CommandHelper.Play(Path, Device);
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }

            return "Ok";
        }

        [Alias("s")]
        [Desc("Send channel message")]
        public string Send([Consume] string Message)
        {
            if (!client.Connected)
                return "Not connected!";

            client.SendMessage(Message, TextMessageTargetMode.Channel, 0);

            return "Ok";
        }

        [Alias("sp")]
        [Desc("Send private message")]
        public string SendPrivate(int ClientId, [Consume] string Message)
        {
            if (!client.Connected)
                return "Not connected!";

            client.SendPrivateMessage(Message, (ushort)ClientId);

            return "Ok";
        }
    }
}
