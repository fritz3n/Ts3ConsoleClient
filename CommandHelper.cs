using FragLabs.Audio.Codecs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TS3Client.Full;
using TS3Client.Messages;
using NAudio;

namespace TS3Client
{
    static class CommandHelper
    {
        static public bool ConsColor = false;
        static public Ts3FullClient client;
        static public bool mic = false; 

        public static string Escape(string text)
        { 
            try
            {
                string pattern = @"\\(\S)";

                Regex rgx = new Regex(pattern);
                return rgx.Replace(text, "$1");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return text;
            }
        }

        public static void ResetColor()
        {
            ConsColor = false;
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void AlternateColor()
        {
            ConsColor = !ConsColor;
            if (ConsColor)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Yellow;
        }

        static WaveIn waveIn;
        static WaveOut _waveOut;
        static BufferedWaveProvider _playBuffer;
        static OpusEncoder encoder;
        static Opusencoder decoder; 
        static int _segmentFrames;
        static int _bytesPerSegment;
        static ulong _bytesSent;

        public static void StartMic(int Device = 0)
        {
            _bytesSent = 0;
            _segmentFrames = 960;
            encoder = OpusEncoder.Create(48000, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
            encoder.Bitrate = 20000;
            //decoder = OpusDecoder.Create(48000, 1);
            _bytesPerSegment = encoder.FrameByteCount(_segmentFrames);

            waveIn = new WaveIn(WaveCallbackInfo.FunctionCallback());
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = Device;
            waveIn.DataAvailable += _waveIn_DataAvailable;
            waveIn.WaveFormat = new WaveFormat(48000, 16, 1);
            
            waveIn.StartRecording();


            mic = true;
        }

        static byte[] _notEncodedBuffer = new byte[0];
        private static void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] soundBuffer = new byte[e.BytesRecorded + _notEncodedBuffer.Length];
            for (int i = 0; i < _notEncodedBuffer.Length; i++)
                soundBuffer[i] = _notEncodedBuffer[i];
            for (int i = 0; i < e.BytesRecorded; i++)
                soundBuffer[i + _notEncodedBuffer.Length] = e.Buffer[i];

            int byteCap = _bytesPerSegment;
            int segmentCount = (int)Math.Floor((decimal)soundBuffer.Length / byteCap);
            int segmentsEnd = segmentCount * byteCap;
            int notEncodedCount = soundBuffer.Length - segmentsEnd;
            _notEncodedBuffer = new byte[notEncodedCount];
            for (int i = 0; i < notEncodedCount; i++)
            {
                _notEncodedBuffer[i] = soundBuffer[segmentsEnd + i];
            }

            for (int i = 0; i < segmentCount; i++)
            {
                byte[] segment = new byte[byteCap];
                for (int j = 0; j < segment.Length; j++)
                    segment[j] = soundBuffer[(i * byteCap) + j];
                int len;
                byte[] buff = encoder.Encode(segment, segment.Length, out len);
                client.SendAudio(buff, len, Codec.OpusVoice);
            }
        }

        public static void StopMic()
        {
            try
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
                encoder.Dispose();
                encoder = null;
                mic = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public static void Play(string path, int Device = 0)
        {
            WaveFileReader orgread;
            try
            {
                orgread = new WaveFileReader(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            WaveFloatTo16Provider read;
            
            read = new WaveFloatTo16Provider(orgread.ToSampleProvider().ToMono().ToWaveProvider());
            
            OpusEncoder encoder = OpusEncoder.Create(48000, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
            encoder.Bitrate = 20000;

            while (true) {
                byte[] buff = new byte[100];
                try
                {
                    read.Read(buff, 0, 100);
                }catch {
                    break;
                }
                
                try
                {
                    byte[] encoded = encoder.Encode(buff, 100, out int len);

                    client.SendAudio(encoded, len, Codec.OpusVoice);
                }catch(Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        

    }
}
