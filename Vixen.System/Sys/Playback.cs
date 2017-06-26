using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using Vixen.Execution;
using Vixen.Execution.Context;
using Vixen.Sys.Output;
using Vixen.Sys.Managers;
using Vixen.Sys.State.Execution;
using Vixen.Sys.Instrumentation;
using Vixen.Module.Media;
using Vixen.Services;
using Vixen.Commands;

namespace Vixen.Sys
{
    public class Playback
    {
        private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();
        
        private static Stopwatch _progress = null;
        private static UInt64 _frame = 0, _update = 0;
		private static ICommand[] _cmd = null;

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
		private static Dictionary<Guid, Controller> _controllers;
		private static Dictionary<Guid, UInt64> _controllerFrames;

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

		public static Dictionary<Guid, Controller> Controllers
        {
            get { return _controllers; }
        }

		public static ICommand[] Command
		{
			get { return _cmd; }
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
				_controllers = new Dictionary<Guid, Controller>();
				_controllerFrames = new Dictionary<Guid, UInt64>();
                foreach (var controller in _export.Network)
				{
					var dev = VixenSystem.OutputControllers.Devices.Where(x => x.Name == controller.Name).FirstOrDefault();
					Guid id;
					if (dev != null) {
						id = dev.Id;
						_controllers.Add(id, controller);
						_controllerFrames.Add(id, 0);
					}
                    Logging.Info("Playback Controller " + controller.Index + ": " +
						"{" + id + "} " + controller.Name + " @ " + controller.StartChan + " + " + controller.Channels);
                    if (controller.StartChan + controller.Channels > channels)
                        channels = controller.StartChan + controller.Channels;
                }
                foreach (var filePath in _export.Media)
                {
                    Logging.Info("Media file: " + filePath);
                    ImportMedia(filePath);
                }

                _update = 0;
				_cmd = new ICommand[channels];
				for (int i = 0; i != channels; i++)
					_cmd[i] = new _8BitCommand(0);
                _dataIn = new BinaryReader(_fs);
                if (_progress == null)
                    _progress = new Stopwatch();
                ReadFrame();
                _updateRate.Reset();
				_progress.Reset();
				_frame = 0;
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
        }

        public static event EventHandler PlaybackStarted;
        public static event EventHandler PlaybackEnded;

		private static Thread _thread = null;

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
			_thread = new Thread(_ThreadFunc);
			_thread.Start();
        }

        public static void Stop()
        {
			if (!IsRunning)
				return;
            if (_progress != null)
				_progress.Stop();
			_thread = null;
			_frame = 0;
            if (_media != null)
                foreach (IMediaModuleInstance media in _media)
                    media.Stop();
            if (PlaybackEnded != null)
                PlaybackEnded(null, null);
		}

		public static void Test()
		{
			_export.Resolution = 0;
			Start();
		}

        // 4 bytes header, 1 byte command (set frame), 1 byte stream
        public static byte[] header = { 0xde, 0xad, 0xbe, 0xef, 0x02, 0x00 };

		private static UInt16 ReadHeader()
		{
			var hdr = _dataIn.ReadBytes(header.Length + 2);
			for (int i = 0; i != header.Length; i++)
				if (hdr[i] != header[i])
					throw new Exception("Frame header error @" + (_fs.Position - header.Length + i));
			return (UInt16)(hdr[header.Length] | (hdr[header.Length + 1] << 8));
		}

        private static void ReadFrame()
        {
			UInt16 channels = ReadHeader();
			var data = _dataIn.ReadBytes(channels);
			for (int i = 0; i != channels; i++)
				((_8BitCommand)_cmd[i]).CommandValue = data[i];
			_playbackTime.Set((double)(_frame * _export.Resolution) / 1000.0);
			_frame++;
            _updateRate.Increment();
        }

		private static void SkipFrame()
		{
			UInt16 channels = ReadHeader();
			_dataIn.ReadBytes(channels);
			_frame++;
		}

        public static void UpdateState(Guid id, out bool allowed)
		{
			allowed = false;
			if (!IsLoaded)
				return;
			if (_controllerFrames[id] != _frame) {
				_controllerFrames[id] = _frame;
				allowed = true;
			}
        }

		private static long _nextUpdateTime;

		private static void _ThreadFunc()
		{
			_nextUpdateTime = _progress.ElapsedMilliseconds + (long)_export.Resolution;
			while (_progress.IsRunning) {
				var sleep = _nextUpdateTime - _progress.ElapsedMilliseconds;
				if (_export.Resolution == 0)
					sleep = 0;
				if (sleep > 0)
					Thread.Sleep((int)sleep);
				try {
					if (sleep < 0)
						SkipFrame();
					else
						ReadFrame();
				} catch (Exception e) {
					Stop();
					break;
				}
				_nextUpdateTime += (long)_export.Resolution;
			}
		}
    }
}
