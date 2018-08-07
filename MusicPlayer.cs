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
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;

namespace TS3Client
{
    public enum State
    {
        Stopped,
        Paused,
        Playing
    }

    public enum PlayerState
    {
        Running,
        Stopped
    }

    static class MusicPlayer
    {
        static Ts3FullClient client;
        static public Context context;
        static AudioFileReader fileReader;
        static ISampleProvider read;
        static private OpusEncoder encoder;
        static private float cachedVol = MapVolume(50);
        static public List<Audio> cue { private set; get; } = new List<Audio>();
        static private Audio current;

        static private CancellationToken ct;
        static private CancellationTokenSource tokenSource;
        static public bool Playing { get; private set; }
        static public State stateInternal = State.Stopped;
        static public State State {
            get { return stateInternal; } set {
                switch (value)
                {
                    case State.Playing:
                        paused = false;
                        StartPlayer();
                        break;

                    case State.Paused:
                        paused = true;
                        break;

                    case State.Stopped:
                        paused = false;
                        StopMusic();
                        break;
                }

                stateInternal = value;
                    } }
        static public PlayerState PlayerState { get; private set; } = PlayerState.Stopped;
        static private bool paused = false;

        static public int Volume { get {
                if (fileReader != null)
                {
                    return UnMapVolume(fileReader.Volume);
                }
                else
                {
                    return UnMapVolume(cachedVol);
                }
            } set {
                if (read != null)
                {
                    fileReader.Volume = MapVolume(value) ;
                }
                cachedVol = MapVolume(value);
            } }

        public static void Init(Ts3FullClient Client)
        {
            client = Client;
            encoder = new OpusEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_AUDIO);
            encoder.Bitrate = 50000;
        }

        private static float MapVolume(int Vol)
        {
            float tmp = Vol;
            return (float)Math.Pow(tmp,2)/10000;
        }

        private static int UnMapVolume(float Vol)
        {
            return (int)Math.Sqrt(Vol * 10000);
        }

        static public WaveFormatCon;
        static Provider convert;

        static public IWaveProvider prov;

        private static void PlayWorker()
        {
            Stopwatch time = new Stopwatch();
            time.Start();
            int sample = 0;
            int sampleTime = 960 / (read.WaveFormat.SampleRate / 1000);

            PlayerState = PlayerState.Running;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (paused)
                    SpinWait.SpinUntil(() => { return !paused; });

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
                    //Console.WriteLine(e);
                    break;
                }
            }
            
            PlayerState = PlayerState.Stopped;
            if (State != State.Stopped)
            {
                ShiftCue();
                State = State.Playing;
            }
        }

        static public void StopMusic()
        {
            if (State == State.Stopped)
                return;

            tokenSource.Cancel();
            current.Close();
        }

        public static string CueAudio(string Url, int Override)
        {
            if(cue.Count >= Override)
            {
                cue[Override].Dispose();
                cue[Override] = new Audio(Url);
                return "Cued " + cue[Override].VideoInfo();
            }
            else
            {
                Cue(new Audio(Url));
                return "Truncated " + Override + " to " + (cue.Count - 1) + "\nCued " + cue[cue.Count - 1].VideoInfo() ;
            }
        }

        public static string CueAudio(string Url)
        {
            Cue(new Audio(Url));
            return "Cued " + cue[cue.Count - 1].VideoInfo();
            
        }



        private static void Close()
        {
            if(fileReader != null)
            {
                fileReader.Close();
                fileReader.Dispose();
                fileReader = null;

                read = null;
            }

            if (current != null)
            {
                current.Dispose();
                current = null;
            }
        }

        public static string Play(string Url)
        {
            Audio audio = new Audio(Url);

            new Task(() =>
            {
                CueFront(audio);
                State = State.Playing;
            }).Start();

            return "Ok";
        }

        private static void UpdateCue()
        {
            Context con = new Context(true);
            
            if (cue.Count < 1) {
                return;
            }

            foreach(Audio audio in cue)
            {
                if(!audio.Prepared && !audio.Prepairing)
                {
                    con.WriteLine("Prepairing " + audio.VideoInfo() + "...");
                    Task t = new Task(() => { audio.Prepare(); new Context(true).WriteLine(audio.VideoInfo() + " is prepared"); });

                    t.Start();
                    
                    SpinWait.SpinUntil(() => { return audio.Prepairing | audio.Prepared; });

                }
            }

            if(current != cue[0])
            {
                Close();
                current = cue[0];
                
                if (current.Prepairing)
                {
                    con.WriteLine("Still prepairing File...");
                    SpinWait.SpinUntil(() => { return !current.Prepairing; });

                    Thread.Sleep(100);

                    if (!current.Prepared)
                    {
                        con.WriteLine("There was a problem, skipping!");
                        ShiftCue();
                        return;
                    }
                }
                
                fileReader = new AudioFileReader(current.path);

                prov = fileReader.ToSampleProvider().ToMono().ToWaveProvider16();

                convert = new WaveFormatConversionProvider(new WaveFormat(48000, 16, 1), prov);

                read = convert.ToSampleProvider();
                fileReader.Volume = cachedVol;

            }
        }

        public static void StartPlayer()
        {
            if (PlayerState == PlayerState.Running | current == null)
            {
                return;
            }



            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            new Task(() => { PlayWorker(); }, ct).Start();

            new Context(true).WriteLine("Now Playing " + current.VideoInfo());

        }

        public static void ShiftCue()
        {
            Context con = new Context(true);
            Close();
            if (cue.Count > 0)
            {
                cue.RemoveAt(0);
            }
            else
            {
                con.WriteLine("Cue end :/");
            }

            UpdateCue();
        }

        public static void Cue(Audio audio)
        {
            cue.Add(audio);
            UpdateCue();
        }

        public static void Cue(int index, Audio audio)
        {
            cue.Insert(index, audio);
            UpdateCue();
        }

        public static void CueFront(Audio audio)
        {
            Cue(0, audio);
        }
    }
    
    class Audio
    {
        int ident;
        public bool Prepared { get; private set; } = false;
        public bool Prepairing { get; private set; } = false;
        public bool IsOpen { get; private set; } = false;
        string url = "";
        public ISampleProvider read;
        public string path;
        bool file = false;
        public AudioFileReader fileReader;
        public WaveFormatConversionProvider convert;
        public IWaveProvider prov;
        private string videoInfo = null;

        public Audio(string Url,  bool Prepare = false)
        {
            Random random = new Random();
            ident = random.Next(1, 1000);

            if (File.Exists(Url))
                file = true;
            
            if (file)
            {
                path = Url;
                Prepared = true;
            }
            else
            {
                url = Url;
            }
        }

        public bool Prepare()
        {

            if (Prepared | file)
            {
                return true;
            }
            
            try
            {
                Prepairing = true;
                Process p = new Process();

                p.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C youtube-dl.exe {url} --proxy 163.172.27.213:3128 --extract-audio -f m4a -o tmp{ ident }.tmp",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                p.Start();
                p.WaitForExit();
                
                if (p.HasExited)
                {
                    Prepared = true;
                    path = $"tmp{ ident }.m4a";
                    Prepairing = false;
                    return true;
                }
                else
                {
                    Prepared = false;
                    Prepairing = false;
                    return false;
                }

            }
            catch
            {
                Prepared = false;
                return false;
            }
        }
        

        public void Close()
        {
            if (!IsOpen)
                return;

            fileReader.Close();
            fileReader.Dispose();
            fileReader = null;

            read = null;

            IsOpen = false;
        }

        public string VideoInfo()
        {
            if(videoInfo != null)
            {
                return videoInfo;
            }

            if (file)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                videoInfo = $"[u]{ name }[/u].";
                return $"[u]{ name }[/u].";
            }

            try
            {
                Regex regex = new Regex(@"(?:\?v=|be\/)([\w\-]+)");
                Match match = regex.Match(this.url);

                if (!match.Success)
                {
                    return null;
                }

                string id = match.Groups[1].Captures[0].Value;

                //string url = $"https://www.googleapis.com/youtube/v3/videos?id={ id }&part=snippet&key=" + Keys.googleOAuth;
                string url = "";
                WebRequest request = WebRequest.Create(url);

                string response = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

                var json = JObject.Parse(response);

                if (json.GetValue("error") != null)
                    return null;

                if (!json["items"].HasValues || !json["items"][0].HasValues)
                    return null;


                var item = json["items"][0]["snippet"];

                string title = item.Value<string>("title");
                string description = item.Value<string>("description");
                string channel = item.Value<string>("channelTitle");
                string channelId = item.Value<string>("channelId");

                string returnVal = $"[url=https://youtu.be/{ id }]{ title }[/url] by [url=https://www.youtube.com/channel/{ channelId }]{channel}[/url]";

                videoInfo = returnVal;

                return returnVal;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "Not Ok";
            }

        }

        public void Dispose()
        {
            if (Prepared)
            {
                if (!file && File.Exists(path))
                {
                    File.Delete(path);
                    ident = 0;
                }
            }
        }

        ~Audio()
        {
            Dispose();
        }
    }
}
