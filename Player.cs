using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using TS3Client.Full;
using TS3Client.Messages;

namespace TS3Client
{
    static class Player
    {
        static private Dictionary<int,BufferedWaveProvider> players;
        static public Ts3FullClient client;
        static private OpusDecoder decoder;
        private static bool Active = false;
        private static int DeviceId;
        private static MixingSampleProvider mixer;
        private static WaveOut Out;

        public static void Start(int Device)
        {
            DeviceId = Device;
            byte[] silence = new byte[10000];
            Array.Clear(silence,0,5000);
            client.PlayVoice = true;
            decoder = OpusDecoder.Create(48000, 1);
            players = new Dictionary<int,BufferedWaveProvider>();

            foreach (ClientData data in client.ClientList())
            {
                BufferedWaveProvider player = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
                players[data.ClientId] = player;
                player.AddSamples(silence,0,5000);
            }

            mixer = new MixingSampleProvider(players.Select((playr) => playr.Value.ToSampleProvider() ));

            Out = new WaveOut();
            Out.DeviceNumber = Device;
            Out.Init(mixer);
            Out.Play();

            Active = true;
        }

        public static void Stop()
        {
            players.Clear();
            client.PlayVoice = false;
            decoder = null;
            Active = false;
        }

        public static void processVoice(IncomingPacket Packet)
        {
            MemoryStream stream = new MemoryStream(Packet.Data);

            BinaryReader reader = new BinaryReader(stream);
            reader.ReadInt16();
            byte[] bytes = reader.ReadBytes(2).Reverse().ToArray();
            ushort Id = BitConverter.ToUInt16(bytes, 0);
            reader.ReadByte();

            byte[] data = new byte[stream.Length];
            stream.Position = 5;
            stream.Read(data, 0, (int)stream.Length);
            try
            {
                short[] decoded = new short[2000];
                int Len = decoder.Decode(data, 0, data.Length, decoded, 0, 2000);

                byte[] outBuff = ShortsToBytes(decoded, 0, Len);

                players[Id].AddSamples(outBuff, 0, Len * 2);
            }
            catch { }
        }

        public static void ReceiveVoice(IncomingPacket Packet)
        {
            new Task(() => { processVoice(Packet); }).Start();
        }

        public static byte[] ShortsToBytes(short[] input)
        {
            return ShortsToBytes(input, 0, input.Length);
        }
        
        public static byte[] ShortsToBytes(short[] input, int offset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            for (int c = 0; c < length; c++)
            {
                processedValues[c * 2] = (byte)(input[c + offset] & 0xFF);
                processedValues[c * 2 + 1] = (byte)((input[c + offset] >> 8) & 0xFF);
            }

            return processedValues;
        }

        public static void Connected(ushort Id)
        {
            if (Active)
            {
                byte[] silence = new byte[10000];
                Array.Clear(silence, 0, 5000);
                BufferedWaveProvider player = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
                players[Id] = player;
                player.AddSamples(silence, 0, 5000);
                mixer.AddMixerInput(player.ToSampleProvider());
            }
        }

        public static void Disconnected(ushort Id)
        {
            if (Active)
            {
                //players[Id].Dispose();
                players.Remove(Id);
            }
        }
    }
}
