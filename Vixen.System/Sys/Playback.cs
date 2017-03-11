using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Xml;
using Vixen.Execution;
using Vixen.Execution.Context;
using Vixen.Sys.Managers;
using Vixen.Sys.State.Execution;
using System.Collections.Concurrent;
using Vixen.Sys.Instrumentation;
using Vixen.Module.Media;
using Vixen.Services;

namespace Vixen.Sys
{
    public class Playback
    {
        private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();

        private static uint _resolution = 50;
        private static string _outFile = null;
        private static TimeSpan _duration;

        private static Stopwatch _progress = null;
        private static UInt64 _frame = 0, _update = 0;
        private static byte[] _data = null;

        private static FileStream _fs = null;
        private static BinaryReader _dataIn = null;
        
        public static List<IMediaModuleInstance> Media { get; set; }

        public struct Controller
        {
            public string name;
            public int index;
            public int startChan;
            public int channels;
        }

        private static Dictionary<string, Controller> _controller;
        
        public static bool IsLoaded
        {
            get { return _dataIn != null; }
        }

        public static bool IsRunning
        {
            get
            {
                if (!IsLoaded)
                    return false;
                return _progress.IsRunning;
            }
        }

        public static Dictionary<string, Controller> Controllers
        {
            get { return _controller; }
        }

        public static byte[] Data
        {
            get { return _data; }
        }

        public static string StatusString
        {
            get
            {
                if (!IsLoaded)
                    return "Unloaded";
                else if (!IsRunning)
                    return "Stopped";
                return "Running: " + TimeSpan.FromMilliseconds(_frame * _resolution).ToString() +
                    " @" + _frame + " " + 100l * _fs.Position / _fs.Length + "% " + _update;
            }
        }
        
        private static RefreshRateValue _updateRate;
        private static TimeValue _playbackTime = null;

        public static void ImportMedia(string filePath)
        {
            IMediaModuleInstance media = MediaService.Instance.ImportMedia(filePath);
            if (media != null)
            {
                Media.Add(media);
                Logging.Info("Media file: " + media.MediaFilePath);
            }
        }

        public static void Load(string fileName)
        {
            // Instrumentation values
            if (_playbackTime == null) {
                _updateRate = new RefreshRateValue("Data dump playback update");
                VixenSystem.Instrumentation.AddValue(_updateRate);
                _playbackTime = new TimeValue("Data dump playback");
                VixenSystem.Instrumentation.AddValue(_playbackTime);
            }

            if (Media == null)
                Media = new List<IMediaModuleInstance>();

            if (IsLoaded)
            {
                Stop();
                Media.Clear();
                if (_fs != null)
                {
                    _fs.Close();
                    _fs = null;
                }
                if (_controller != null)
                {
                    _controller.Clear();
                    _controller = null;
                }
                _dataIn = null;
                _data = null;
            }

            if (fileName == null)
                return;

            try {
                _controller = new Dictionary<string, Controller>();
                XmlReader reader = XmlReader.Create(fileName);
                int channels = 0;
                while (reader.Read())
                    if (reader.IsStartElement() && !reader.IsEmptyElement)
                        switch (reader.Name) {
                        case "Resolution":
                            _resolution = (uint)reader.ReadElementContentAsInt();
                            Logging.Info("Playback Resolution: " + _resolution);
                            break;
                        case "OutFile":
                            _outFile = reader.ReadElementContentAsString();
                            _fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(fileName), _outFile));
                            Logging.Info("Playback OutFile: " + _outFile);
                            break;
                        case "Duration":
                            _duration = TimeSpan.Parse(reader.ReadElementContentAsString());
                            Logging.Info("Playback Duration: " + _duration.ToString());
                            break;
                        case "Network":
                            while (reader.Read()) {     // Nested Network element
                                if (!reader.IsStartElement())
                                    break;
                                if (reader.Name != "Controller")
                                    continue;
                                Controller con = new Controller();
                                while (reader.Read()) {
                                    if (!reader.IsStartElement())
                                        break;
                                    switch (reader.Name) {
                                    case "Index":
                                        con.index = reader.ReadElementContentAsInt();
                                        break;
                                    case "Name":
                                        con.name = reader.ReadElementContentAsString();
                                        break;
                                    case "StartChan":
                                        con.startChan = reader.ReadElementContentAsInt();
                                        break;
                                    case "Channels":
                                        con.channels = reader.ReadElementContentAsInt();
                                        break;
                                    }
                                }
                                if (con.startChan + con.channels > channels)
                                    channels = con.startChan + con.channels;
                                _controller.Add(con.name, con);
                                Logging.Info("Playback Controller " + con.index + ": " +
                                    con.name + " @ " + con.startChan + " + " + con.channels);
                            }
                            break;
                        case "Media":
                            while (reader.Read())
                            {     // Nested Media element
                                if (!reader.IsStartElement())
                                    break;
                                if (reader.Name != "FilePath")
                                    continue;
                                ImportMedia(reader.ReadElementContentAsString());
                            }
                            break;
                        }
                reader.Close();

                _frame = 0;
                _update = 0;
                _data = new byte[channels];
                _dataIn = new BinaryReader(_fs);
                if (_progress == null)
                    _progress = new Stopwatch();
                ReadFrame();
                _updateRate.Reset();
                _progress.Reset();
            } catch (Exception e) {
                _dataIn = null;
                throw e;
            }
        }

        public static void Start()
        {
            if (!IsLoaded || IsRunning)
                return;
            foreach (IMediaModuleInstance media in Media)
                media.LoadMedia(_progress.Elapsed);
            foreach (IMediaModuleInstance media in Media)
                media.Start();
            _progress.Start();
        }

        public static void Stop()
        {
            if (!IsLoaded)
                return;
            _progress.Stop();
            foreach (IMediaModuleInstance media in Media)
                media.Stop();
        }

        // 4 bytes header, 1 byte command (set frame), 1 byte stream
        public static byte[] header = { 0xde, 0xad, 0xbe, 0xef, 0x02, 0x00 };

        private static void ReadFrame()
        {
            try {
                int i = 0;
                while (i != header.Length) {
                    byte c = _dataIn.ReadByte();
                    if (c != header[i++]) {
                        i = c == header[0] ? 1 : 0;
                        Logging.Warn("Frame header error @" + _fs.Position);
                    }
                }
                UInt16 channels = _dataIn.ReadUInt16();
                Array.Copy(_dataIn.ReadBytes(channels), _data, channels);
                _playbackTime.Set((double)(_frame * _resolution) / 1000.0);
                _updateRate.Increment();
            } catch (Exception e) {
                Stop();
            }
        }

        private static Object lockObject = new Object();

        public static void UpdateState(out bool allowed)
        {
            lock (lockObject) {
                if (!IsLoaded) {
                    allowed = false;
                    return;
                }
                _update++;
                //bool allowUpdate = _UpdateAdjudicator.PetitionForUpdate();
                UInt64 itvl = (ulong)_progress.ElapsedMilliseconds / _resolution - _frame;
                allowed = itvl != 0ul;
                if (allowed)
                    while (itvl-- != 0ul) {
                        _frame++;
                        ReadFrame();
                    }
            }
        }
    }
}
