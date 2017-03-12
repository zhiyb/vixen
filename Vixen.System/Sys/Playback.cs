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
using System.Xml.Serialization;

namespace Vixen.Sys
{
    public class Playback
    {
        private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();
        
        private static Stopwatch _progress = null;
        private static UInt64 _frame = 0, _update = 0;
        private static byte[] _data = null;

        private static FileStream _fs = null;
        private static BinaryReader _dataIn = null;

        private static List<IMediaModuleInstance> _media = null;

        [XmlRoot("Vixen3_Export")]
        public class Export
        {
            [XmlElement("Resolution")]
            public ulong Resolution { get; set; }
            [XmlElement("OutFile")]
            public string OutFile { get; set; }
            [XmlElement("Duration")]
            public string Duration { get; set; }
            [XmlArray("Network")]
            public List<Controller> Network { get; set; }
            [XmlArray("Media")]
            [XmlArrayItem("FilePath")]
            public List<string> Media { get; set; }
        }

        [XmlType("Controller")]
        public class Controller
        {
            [XmlElement("Index")]
            public int Index { get; set; }
            [XmlElement("Name")]
            public string Name { get; set; }
            [XmlElement("StartChan")]
            public int StartChan { get; set; }
            [XmlElement("Channels")]
            public int Channels { get; set; }
        }

        private static Export _export = null;
        private static Dictionary<string, Controller> _controllers;

        public static bool IsLoaded
        {
            get { return _dataIn != null; }
        }

        public static bool IsRunning
        {
            get
            {
                if (!IsLoaded || _progress == null)
                    return false;
                return _progress.IsRunning;
            }
        }

        public static Dictionary<string, Controller> Controllers
        {
            get { return _controllers; }
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
                return "Running: " + TimeSpan.FromMilliseconds(_frame * _export.Resolution).ToString() +
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
                media.LoadMedia(TimeSpan.Zero);
                _media.Add(media);
            }
        }

        public static void Load(string fileName)
        {
            // Instrumentation values
            if (_playbackTime == null)
            {
                _updateRate = new RefreshRateValue("Data dump playback update");
                VixenSystem.Instrumentation.AddValue(_updateRate);
                _playbackTime = new TimeValue("Data dump playback");
                VixenSystem.Instrumentation.AddValue(_playbackTime);
            }

            if (_media == null)
                _media = new List<IMediaModuleInstance>();

            if (IsLoaded)
                Unload();

            if (fileName == null)
                return;
            
            try
            {
                var serializer = new XmlSerializer(typeof(Export));
                XmlReader reader = XmlReader.Create(fileName);
                _export = (Export)serializer.Deserialize(reader);
                reader.Close();

                Logging.Info("Playback Resolution: " + _export.Resolution);
                Logging.Info("Playback OutFile: " + _export.OutFile);
                _fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(fileName), _export.OutFile));
                Logging.Info("Playback Duration: " + _export.Duration);
                int channels = 0;
                _controllers = new Dictionary<string, Controller>();
                foreach (var controller in _export.Network)
                {
                    Logging.Info("Playback Controller " + controller.Index + ": " +
                        controller.Name + " @ " + controller.StartChan + " + " + controller.Channels);
                    if (controller.StartChan + controller.Channels > channels)
                        channels = controller.StartChan + controller.Channels;
                    _controllers.Add(controller.Name, controller);
                }
                foreach (var filePath in _export.Media)
                {
                    Logging.Info("Media file: " + filePath);
                    ImportMedia(filePath);
                }

                _frame = 0;
                _update = 0;
                _data = new byte[channels];
                _dataIn = new BinaryReader(_fs);
                if (_progress == null)
                    _progress = new Stopwatch();
                ReadFrame();
                _updateRate.Reset();
                _progress.Reset();
                foreach (IMediaModuleInstance media in _media)
                    media.LoadMedia(_progress.Elapsed);
            }
            catch (Exception e)
            {
                Unload();
                throw e;
            }
        }

        public static void Unload()
        {
            Stop();
            if (_media != null)
                _media.Clear();
            if (_fs != null)
            {
                _fs.Close();
                _fs = null;
            }
            if (_controllers != null)
            {
                _controllers.Clear();
                _controllers = null;
            }
            _dataIn = null;
            _data = null;
        }

        public static event EventHandler PlaybackStarted;
        public static event EventHandler PlaybackEnded;

        public static void Start()
        {
            if (!IsLoaded || IsRunning)
                return;
            foreach (IMediaModuleInstance media in _media)
                media.LoadMedia(_progress.Elapsed);
            foreach (IMediaModuleInstance media in _media)
                media.Start();
            _progress.Start();
            if (PlaybackStarted != null)
                PlaybackStarted(null, null);
        }

        public static void Stop()
        {
            if (_progress != null)
                _progress.Stop();
            if (_media != null)
                foreach (IMediaModuleInstance media in _media)
                    media.Stop();
            if (PlaybackEnded != null)
                PlaybackEnded(null, null);
        }

        // 4 bytes header, 1 byte command (set frame), 1 byte stream
        public static byte[] header = { 0xde, 0xad, 0xbe, 0xef, 0x02, 0x00 };

        private static void ReadFrame()
        {
            try
            {
                int i = 0;
                while (i != header.Length)
                {
                    byte c = _dataIn.ReadByte();
                    if (c != header[i++])
                    {
                        i = c == header[0] ? 1 : 0;
                        Logging.Warn("Frame header error @" + _fs.Position);
                    }
                }
                UInt16 channels = _dataIn.ReadUInt16();
                Array.Copy(_dataIn.ReadBytes(channels), _data, channels);
                _playbackTime.Set((double)(_frame * _export.Resolution) / 1000.0);
                _updateRate.Increment();
            }
            catch (Exception e)
            {
                Stop();
            }
        }

        private static Object lockObject = new Object();

        public static void UpdateState(out bool allowed)
        {
            lock (lockObject)
            {
                if (!IsLoaded)
                {
                    allowed = false;
                    return;
                }
                _update++;
                //bool allowUpdate = _UpdateAdjudicator.PetitionForUpdate();
                UInt64 itvl = (ulong)_progress.ElapsedMilliseconds / _export.Resolution - _frame;
                allowed = itvl != 0ul;
                if (allowed)
                    while (itvl-- != 0ul)
                    {
                        _frame++;
                        ReadFrame();
                    }
            }
        }
    }
}
