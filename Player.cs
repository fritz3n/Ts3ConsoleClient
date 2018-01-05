using FragLabs.Audio.Codecs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3Client.Full;
using TS3Client.Messages;

namespace TS3Client
{
    static class Player
    {
        static private Dictionary<int,player> players;
        static public Ts3FullClient client;
        static private Opusencoder decoder;
        private static bool Active = true;
        private static int DeviceId;

        public static void Start(int Device)
        {
            DeviceId = Device;
            byte[] silence = new byte[10000];
            Array.Clear(silence,0,5000);
            client.PlayVoice = true;
            decoder = Opusencoder.Create(48000, 1);
            players = new Dictionary<int,player>();

            foreach (ClientData data in client.ClientList())
            {
                player player = new player(new WaveFormat(48000, 16, 1), Device);
                players[data.ClientId] = player;
                player.buffProv.AddSamples(silence,0,5000);
            }
            Active = true;
        }

        public static void Stop()
        {
            foreach(KeyValuePair<int,player> ply in players)
            {
                player player = ply.Value;
                player.Dispose();
            }

            players.Clear();
            client.PlayVoice = false;
            decoder.Dispose();
            decoder = null;
            Active = false;
        }

        public static void ReceiveVoice(IncomingPacket Packet)
        {
            MemoryStream stream = new MemoryStream(Packet.Data);

            BinaryReader reader = new BinaryReader(stream);
            reader.ReadInt16();
            byte[] bytes = reader.ReadBytes(2).Reverse().ToArray();
            ushort Id = BitConverter.ToUInt16(bytes,0);
            reader.ReadByte();

            byte[] data = new byte[stream.Length];
            stream.Position = 5;
            stream.Read(data,0,(int)stream.Length);
            try
            {
                int Len;
                byte[] decoded = decoder.Decode(data, data.Length, out Len);

                players[Id].buffProv.AddSamples(decoded, 0, Len);
            }
            catch { }
        }

        public static void Connected(ushort Id)
        {
            byte[] silence = new byte[10000];
            Array.Clear(silence, 0, 5000);
            player player = new player(new WaveFormat(48000, 16, 1), DeviceId);
            players[Id] = player;
            player.buffProv.AddSamples(silence, 0, 5000);
        }

        public static void Disconnected(ushort Id)
        {
            players[Id].Dispose();
            players.Remove(Id);
        }
    }

    class player{
        public BufferedWaveProvider buffProv;
        public WaveOut waveOut;

        public player(WaveFormat Format,int Device)
        {
            buffProv = new BufferedWaveProvider(Format);
            waveOut = new WaveOut();
            waveOut.DeviceNumber = Device;
            waveOut.Init(buffProv);

            waveOut.Play();
        }

        public void Dispose()
        {
            buffProv = null;
            waveOut.Stop();
            waveOut.Dispose();
            waveOut = null;
        }
    }
}
