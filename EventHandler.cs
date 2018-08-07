using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3Client.Full;
using TS3Client.Messages;

namespace TS3Client
{
    static class TsEventHandler
    {
        static Ts3FullClient client;

        public static void StartEventHandler(Ts3FullClient clien)
        {
            client = clien;

            client.OnConnected += Client_OnConnected;
            client.OnClientEnterView += Client_OnClientEnterView;
            client.OnClientLeftView += Client_OnClientLeftView;
            client.OnTextMessageReceived += Client_OnTextMessageReceived;
            client.OnClientMoved += Client_OnClientMoved;
            client.OnErrorEvent += Client_OnErrorEvent;
        }

        private static void Client_OnErrorEvent(object sender, CommandError e)
        {
            ConsoleHelper.WriteEventLine(e.ErrorFormat(), Color.ForestGreen);
        }

        public static Dictionary<ushort, ClientData> Clients = new Dictionary<ushort, ClientData>();

        public static void Client_OnConnected(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        public static void Client_OnClientEnterView(object sender, IEnumerable<ClientEnterView> e)
        {
            foreach (ClientEnterView Client in e)
            {
                try
                {
                    if (Client.ClientType == ClientType.Full)
                    {
                        Player.Connected(Client.ClientId);
                        ConsoleHelper.WriteEventLine(Client.NickName + " entered view!(" + Client.Reason + ")", Color.ForestGreen);
                    }
                    Clients.Add(Client.ClientId, client.ClientList().Where((o) => o.ClientId == Client.ClientId).SingleOrDefault<ClientData>());
                }
                catch { }
            }
        }

        public static void Client_OnClientLeftView(object sender, IEnumerable<ClientLeftView> e)
        {
            foreach (ClientLeftView Client in e)
            {
                try
                {
                    if (Clients.ContainsKey(Client.ClientId))
                    {
                        if (Clients[Client.ClientId].ClientType == ClientType.Full)
                        {
                            Player.Connected(Client.ClientId);
                            ConsoleHelper.WriteEventLine(Clients[Client.ClientId].NickName + " left view!(" + Client.Reason + ")", Color.ForestGreen);
                        }
                        Clients.Remove(Client.ClientId);
                    }
                }
                catch { }
            }
        }

        public static void Client_OnTextMessageReceived(object sender, IEnumerable<TextMessage> e)
        {
            foreach (TextMessage Message in e)
            {
                Color col = Color.White;

                if (Message.Target == TextMessageTargetMode.Private)
                    col = Color.LightGreen;

                if (Message.Target == TextMessageTargetMode.Channel)
                    col = Color.Cyan;

                ConsoleHelper.WriteEventLine(Message.InvokerName + ": " + Message.Message, col);

                if (Message.InvokerId != client.ClientId && Message.Message.Trim().StartsWith("!b"))
                {
                    string[] ar = Message.Message.Split(new char[] { ' ' }, 2);
                    string Msg = ar[1].TrimStart().Replace("[URL]","").Replace("[/URL]", "");

                    ConsoleHelper.WriteEventLine("Executing: " + Msg, Color.Orange);

                    new Task(() => { AsyncComHandler.HandleCommand(Msg, new Context(Message, client));  }).Start();
                }
            }
        }

        public static void Client_OnClientMoved(object sender, IEnumerable<ClientMoved> e)
        {
            //throw new NotImplementedException();
        }
    }
}
