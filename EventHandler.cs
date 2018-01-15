using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3Client.Full;
using TS3Client.Messages;
using AsyncCommandHandler;

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
                if (Client.ClientType == ClientType.Full)
                {
                    ConsoleHelper.WriteEventLine(Client.NickName + " entered view!", Color.Red);
                }
                Clients.Add(Client.ClientId, client.ClientList().Where((o) => o.ClientId == Client.ClientId).Single<ClientData>());
                
            }
        }

        public static void Client_OnClientLeftView(object sender, IEnumerable<ClientLeftView> e)
        {
            foreach (ClientLeftView Client in e)
            {
                if (Clients[Client.ClientId].ClientType == ClientType.Full)
                {
                    ConsoleHelper.WriteEventLine(Clients[Client.ClientId].NickName + " left view!", Color.Red);
                }
                Clients.Remove(Client.ClientId);
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
                //Handle the Command:
                //AsyncComHandler.HandleCommand(Message.Message);
            }
        }

        public static void Client_OnClientMoved(object sender, IEnumerable<ClientMoved> e)
        {
            //throw new NotImplementedException();
        }
    }
}
