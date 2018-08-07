using Concentus;
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
using NAudio.Wave.SampleProviders;
using Concentus.Structs;
using System.Threading;
using System.Diagnostics;

namespace TS3Client
{
    static class CommandHelper
    {
        static public bool ConsColor = false;
        static public Ts3FullClient client;
        static public bool mic = false;
        static Context context = new Context();

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
        static OpusEncoder encoder;
        static int _bytesPerSegment = 1920;

        public static void StartMic(int Device = 0)
        {
            encoder = new OpusEncoder(48000, 1,Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 50000;
            encoder.Complexity = 8;

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
                
                short[] inbuff = BytesToShorts(segment);

                try
                {
                    byte[] buff = new byte[1275];
                    int len = encoder.Encode(inbuff, 0, 960, buff, 0, 1275);
                    client.SendAudio(buff, len, Codec.OpusVoice);
                }catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static void StopMic()
        {
            try
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
                encoder = null;
                mic = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static short[] BytesToShorts(byte[] input)
        {
            return BytesToShorts(input, 0, input.Length);
        }
        
        private static short[] BytesToShorts(byte[] input, int offset, int length)
        {
            short[] processedValues = new short[length / 2];
            for (int c = 0; c < processedValues.Length; c++)
            {
                processedValues[c] = BitConverter.ToInt16(new byte[] { input[(c * 2) + offset], input[(c * 2) + 1 + offset] }, 0);
            }

            return processedValues;
        }
    }
}
