using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using TS3Client.Full;
using TS3Client;
using TS3Client.Messages;
using TS3Client.Commands;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using NAudio.Wave;
using System.Reflection;

namespace TS3Client
{
    class CommandHandler
    {
        Ts3FullClient client;
        string id = "MG4DAgeAAgEgAiA6LClUWJNGHGDXPdvY9zDBl7uiGUyFYnqUN0KD7xSx/wIhAItdd2gsoToPT6m8ryjb4VfNPEyrrs9L2QgBQFRo2nZ4AiAVizKxZhKHkad6n+uaY/3s2OQ+6V+N9VI9gr2JwI2pGg==";
        public Context context;


        public CommandHandler()
        {
            client = new Ts3FullClient(EventDispatchType.DoubleThread);
            CommandHelper.client = client;
            Player.client = client;
            TsEventHandler.StartEventHandler(client);
            MusicPlayer.Init(client);
            Resources.client = client;
        }

        ~CommandHandler()
        {
            
        }

        [Restriction(false)]
        [Alias("c")]
        [Desc("Connect the bot")]
        public string Connect(string Name = "FaCeBoTt", string Url = "c0d1n6.io")
        {
            Console.WriteLine(Url);

            ConnectionDataFull Join = new ConnectionDataFull();
            

            Join.Address = Url;

            //Join.Identity = Ts3Crypt.LoadIdentity(id, ulong.Parse("97698842"));
            Join.Identity = Ts3Crypt.GenerateNewIdentity();

            Join.Username = Name;
            Join.HWID = "Cock 7";


            client.Connect(Join);

            while (client.status == Ts3FullClient.Ts3ClientStatus.Connecting) { }


            if (client.Connected)
            {
                SetDefaultAvatar();
                return "Ok";
            }
            else
            {
                return "Error!";
            }
        }

        [Restriction(false)]
        [Alias("dc")]
        [Desc("Disconnect the bot")]
        public string Disconnect()
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.State = State.Stopped;
            StopMic();
            StopVoice();

            client.Disconnect();
            return "Ok";
        }

        [Restriction(false)]
        [Alias("close","bye","stop","ex")]
        [Desc("Exit the app")]
        public void Exit()
        {
            Disconnect();
            Environment.Exit(0);
        }

         [Desc("Change the clients name")]
         public string ChangeName([Consume] string name = "FaCeBoTt")
         {
             if (String.IsNullOrEmpty(name))
             {
                 return "Bad String!";
             }
             else
             {
                 client.ChangeName(name);
                return "Ok";
             }
         }

        [Restriction(false)]
        [Desc("Execute a File")]
        public void Execute(string Path)
        {
            string[] lines = File.ReadAllLines(Path);
            AsyncComHandler.execute(lines);
        }
        [Desc("Set Default Avatar")]
        public void SetDefaultAvatar()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TS3Client.png.png";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                client.UploadAvatar(stream);
            }
        }

        public void ChangeAvatar()
        {
            //open a file dialog...
            Stream myStream = null;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Open Picture File";
            dialog.Filter = "picture files|*.jpg;*.jpeg;*.png; |Any File|*.*";
            dialog.InitialDirectory = @"C:\";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = dialog.OpenFile()) != null)
                {
                    using (myStream)
                    {
                        client.UploadAvatar(myStream);
                    }
                }
            }
        }
        public void Chanelname()
        {

        }
        [Desc("List clients")]
        public string List()
        {
            if (!client.Connected)
                return "Not connected!";

            context.BufferOn();
            CommandHelper.ResetColor();
            foreach (ClientData data in client.ClientList())
            {
                CommandHelper.AlternateColor();
                //context.Write(data.ReturnCode);
                context.WriteLine(data.ClientId + "\t" + data.NickName + "\t|");
            
            }
            CommandHelper.ResetColor();
            context.Flush();

            return null;
        }

        [Desc("List Channels")]
        public string ListChannels()
        {
            if (!client.Connected)
                return "Not connected!";

            context.BufferOn();
            List<ResponseDictionary> Channels = client.SendCommand<ResponseDictionary>(new Ts3Command("channellist")).ToList();

            CommandHelper.ResetColor();
            foreach (ResponseDictionary Channel in Channels)
            {
                CommandHelper.AlternateColor();
                context.WriteLine(" " + Channel["cid"] + "\t" + CommandHelper.Escape(Channel["channel_name"].Replace("\\s", " ")));
            }
            CommandHelper.ResetColor();
            context.Flush();

            return null;
        }

        [Restriction(false)]
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

        [Restriction(false)]
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

        [Restriction(false)]
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

            context.WriteLine("Now transmitting from " + WaveIn.GetCapabilities(DeviceNumber).ProductName);
            context.WriteLine("Press c to close and stop the mic.");
            context.WriteLine("Press k to close and keep the mic open.");

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
                        
                }
                
            }

            return "Ok";
        }

        [Restriction(false)]
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

        [Restriction(false)]
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
                context.WriteLine(e);
            }

            return "Ok";
        }

        [Restriction(false)]
        [Alias("sv")]
        [Desc("Stop voice output")]
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
                context.WriteLine(e);
            }

            return "Ok";
        }

        [Restriction(false)]
        [Alias("pl")]
        [Desc("Play an audio File or (Youtube) Url\nPornhub also supported ;)")]
        public string PlayFile(string Path)
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            return MusicPlayer.Play(Path);
        }

        [Restriction(false)]
        [Alias("pl")]
        [Desc("Play the Cue")]
        public string PlayFile()
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            MusicPlayer.State = State.Playing;
            return "Ok";
        }

        [Alias("cu")]
        [Desc("Print current cue")]
        public string Cue()
        {
            if (!client.Connected)
                return "Not connected!";

            context.BufferOn();
            int I = 0;
            foreach(Audio audio in MusicPlayer.cue)
            {
                context.WriteLine(I + ": " + audio.VideoInfo());
                I++;
            }
            context.Flush();

            return null;
        }

        [Alias("cu")]
        [Desc("Cue a song")]
        public string Cue(string Url)
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            MusicPlayer.CueAudio(Url);

            return "Ok";
        }

        [Alias("cu")]
        [Desc("Override cue Item")]
        public string Cue(string Url, int Index)
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            MusicPlayer.CueAudio(Url, Index);

            return "Ok";
        }

        [Alias("sf")]
        [Desc("Stop the Music")]
        public string StopFile()
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            MusicPlayer.State = State.Stopped;

            return "Ok";
        }

        [Alias("vol")]
        [Desc("Set File Playing Volume")]
        public string Volume(int Volume)
        {
            if (!client.Connected)
                return "Not connected!";

            MusicPlayer.context = context;
            MusicPlayer.Volume = Volume;

            return "Ok";
        }
        
        [Restriction(false)]
        [Desc("List Audio Input Devices and their id")]
        public string ListAudioInputs()
        {
            for(int i = 0; i < WaveIn.DeviceCount; i++)
            {
                WaveInCapabilities device = WaveIn.GetCapabilities(i);
                context.WriteLine(i + "\t" + device.ProductName);
            }

            return null;
        }
        
        [Restriction(false)]
        [Desc("List Audio Output Devices and their id")]
        public string ListAudioOutputs()
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities device = WaveOut.GetCapabilities(i);
                context.WriteLine(i + "\t" + device.ProductName);
            }

            return null;
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

    public static class Resources
    {
        static public Ts3FullClient client;
    }

    public class Context
    {
        public bool useTs = false;
        public bool usePrivate = false;
        public ClientData sender;
        public Ts3FullClient client;
        public string buffer;
        private string allBuffer;
        private bool bufferAll;

        public Context(TextMessage Message, Ts3FullClient client)
        {
            if (Message != null && client != null)
            {
                this.client = client;
                useTs = true;
                if(Message.Target == TextMessageTargetMode.Private)
                {
                    usePrivate = true;
                    sender = client.ClientList().Where((o) => Message.InvokerId == o.ClientId).FirstOrDefault();
                }
            }
        }

        public Context(bool UseChat)
        {
            useTs = UseChat;
            if (useTs)
                client = Resources.client;
        }

        public Context()
        {

        }

        public void Write(object obj)
        {
            if (bufferAll)
            {
                allBuffer += obj.ToString();
                return;
            }

            if (useTs)
            {
                buffer += obj.ToString();
            }
            else
            {
                Console.WriteLine(obj);
            }
        }

        public void BufferOn()
        {
            bufferAll = true;
        }

        public void Flush()
        {
            bufferAll = false;

            foreach(string Chunk in ChunksUpto(allBuffer, 1000))
            {
                WriteLine(Chunk);
            }
        }

        private IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public void WriteLine()
        {
            WriteLine(" ");
        }

        public void WriteLine(object obj)
        {
            if (bufferAll)
            {
                allBuffer += obj.ToString() + "\n";
                return;
            }

            if (useTs)
            {
                try
                {
                    if (usePrivate)
                    {
                        client.SendMessage(buffer + obj.ToString(), sender);
                    }
                    else
                    {
                        client.SendChannelMessage(buffer + obj.ToString());
                    }
                }catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(obj.ToString().Length);
                }

                buffer = "";
            }
            else
            {
                Console.WriteLine(obj);
            }
        }
    }
}
