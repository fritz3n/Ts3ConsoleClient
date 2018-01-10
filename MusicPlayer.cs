using Concentus.Structs;
using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS3Client.Full;
using Concentus.Oggfile;
using System.IO;
using System.Text.RegularExpressions;

namespace TS3Client
{
    static class MusicPlayer
    {
        static Ts3FullClient client;
        static Context context;
        static AudioFileReader fileReader;
        static ISampleProvider read;
        static private OpusEncoder encoder;
        static private float cachedVol = 0.6f;

        static private CancellationToken ct;
        static private CancellationTokenSource tokenSource;
        
        static public int Volume { get {
                if (read != null)
                {
                    return (int)fileReader.Volume * 100;
                }
                else
                {
                    return (int)cachedVol * 100;
                }
            } set {
                if (read != null)
                {
                    fileReader.Volume = ((float)value) / 100;
                }
                cachedVol = ((float)value) / 100;
            } }

        public static void Init(Ts3FullClient Client)
        {
            client = Client;
        }

        private static void PlayWorker()
        {
            Stopwatch time = new Stopwatch();
            time.Start();
            int sample = 0;
            int sampleTime = 960 / (read.WaveFormat.SampleRate / 1000);

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                float[] buff = new float[960];
                int byteNum;
                try
                {
                    byteNum = read.Read(buff, 0, 960);
                }
                catch(Exception e)
                {
                    break;
                }

                try
                {
                    byte[] encoded = new byte[1275];
                    int len = encoder.Encode(buff, 0, byteNum, encoded, 0, 1275);

                    sample++;

                    while (!(time.ElapsedMilliseconds >= sample * sampleTime)) ;
                    ct.ThrowIfCancellationRequested();
                    client.SendAudio(encoded, len, Codec.OpusVoice);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }

            StopMusic();
        }

        static public void StopMusic()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            DeleteYtFIle();

            if (fileReader != null)
            {
                fileReader.Close();
                fileReader.Dispose();
                fileReader = null;
            }
        }

        public static void PlayFile(string path)
        {
            try
            {
                fileReader = new AudioFileReader(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
            
            IWaveProvider prov = fileReader.ToMono().ToWaveProvider16();
            
            WaveFormatConversionProvider convert2 = new WaveFormatConversionProvider(new WaveFormat(48000, 16, 1), prov);

            read = convert2.ToSampleProvider();

            fileReader.Volume = cachedVol;

            encoder = new OpusEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_AUDIO);
            encoder.Bitrate = 50000;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            new Task(() => { PlayWorker(); }, ct).Start();
        }

        private static int ident = 0;

        public static void PlayYoutube(string Url, Context context)
        {
            DeleteYtFIle();
            Process p = new Process();

            Random random = new Random();

            ident = random.Next(1, 1000);

            p.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C youtube-dl.exe {Url} --extract-audio -f m4a -o tmp{ ident }.tmp",
                WindowStyle = ProcessWindowStyle.Hidden

            };

            context.WriteLine("Downloading audio...");
            p.Start();

            p.WaitForExit(30000);
            

            PlayFile($"tmp{ ident }.m4a");
        }

        public static void DeleteYtFIle()
        {
            if (ident != 0 && File.Exists($"tmp{ ident }.m4a"))
            {
                File.Delete($"tmp{ ident }.m4a");
                ident = 0;
            }
        }

    }
}
