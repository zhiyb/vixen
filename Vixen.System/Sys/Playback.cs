using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
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
		private static long _nextUpdateTime;

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

		private struct Info
		{
			public enum Types {Invalid, Sequence, Video};
			public Types type;
			public bool audio;
			public long audioTime;
			public IntPtr data;
			public int channels;
			public byte[] chdata;
		}

        private static Export _export = null;
		private static Info _info;
		private static Dictionary<Guid, Controller> _controllers;
		private static Dictionary<Guid, UInt64> _controllerFrames;

        public static bool IsLoaded
        {
			get { return _info.type == Info.Types.Video ? _info.data != null : _dataIn != null; }
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

		private static void ImportMedia(ref Info info, string sequencePath, string filePath, uint sync)
        {
            /*IMediaModuleInstance media = MediaService.Instance.ImportMedia(filePath);
			if (media != null) {
				media.LoadMedia(TimeSpan.Zero);
				_media.Add(media);
			} else*/ {
				// Find media file
				if (!File.Exists(filePath)) {
					var fileName = Path.GetFileName(filePath);
					filePath = Path.Combine(Path.GetDirectoryName(sequencePath), fileName);
					if (!File.Exists(filePath)) {
						filePath = Path.Combine(MediaService.MediaDirectory, fileName);
						if (!File.Exists(filePath))
							return;
					}
				}

				// Try use PlaybackCodec
				IntPtr cmtp = new IntPtr();
				int gota, gotv;
				IntPtr data = PlaybackCodec.codec_alloc();
				if (data == IntPtr.Zero)
					return;
				if (PlaybackCodec.decode_open_input(data, filePath, ref cmtp, out gota, out gotv) == 0)
					return;
				if (gota == 0) {
					PlaybackCodec.decode_close(data);
					return;
				}
				CodecClose(ref info);
				info.audio = false;
				info.audioTime = 0;
				if (PlaybackCodec.fmod_init(data) == 0)
					if (PlaybackCodec.fmod_create_stream(data, data, sync) == 0) {
						info.audio = true;
						info.data = data;
						return;
					}
				PlaybackCodec.decode_close(data);
			}
        }

		private static int LoadExportInfo()
		{
			Logging.Info("Playback Resolution: " + _export.Resolution);
			Logging.Info("Playback OutFile: " + _export.OutFile);
			Logging.Info("Playback Duration: " + _export.Duration);
			int channels = 0;
			_controllers = new Dictionary<Guid, Controller>();
			_controllerFrames = new Dictionary<Guid, UInt64>();
			foreach (var controller in _export.Network)
			{
				var dev = VixenSystem.OutputControllers.Devices.Where(x => x.Name == controller.Name).FirstOrDefault();
				Guid id = Guid.Empty;
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
			return channels;
		}

		private static void LoadSequence(ref Info info, string fileName)
		{
			Logging.Info("Playback loading sequence: " + fileName);
			try
			{
				var reader = XmlReader.Create(fileName);
				var serializer = new XmlSerializer(typeof(Export));
				_export = (Export)serializer.Deserialize(reader);
				reader.Close();

                // Open RAW sequence file
				_fs = File.OpenRead(Path.Combine(Path.GetDirectoryName(fileName), _export.OutFile));
				_dataIn = new BinaryReader(_fs);
                // Open audio stream if available
				info.audio = false;
				foreach (var filePath in _export.Media)
				{
					Logging.Info("Media file: " + filePath);
					ImportMedia(ref info, fileName, filePath, (uint)_export.Resolution);
				}

				info.channels = LoadExportInfo();
				info.type = Info.Types.Sequence;
				Initialise(info.channels);
			}
			catch (Exception e)
			{
				Unload();
				throw e;
			}
		}

		private static void LoadVideo(ref Info info, string fileName)
		{
			Logging.Info("Playback loading video: " + fileName + ", codec version " + PlaybackCodec.codec_version());
			// Open video file for decoding input
			IntPtr cmtp = new IntPtr();
			int gota, gotv;
			IntPtr data = PlaybackCodec.codec_alloc();
			if (data == IntPtr.Zero)
				return;
			if (PlaybackCodec.decode_open_input(data, fileName, ref cmtp, out gota, out gotv) == 0)
				return;
			if (gotv == 0) {
				PlaybackCodec.decode_close(data);
				return;
			}

            // Deserialise XML controller configuration
            string strXml = Marshal.PtrToStringAnsi(cmtp);
            var reader = new StringReader(strXml);
			var serializer = new XmlSerializer(typeof(Export));
			_export = (Export)serializer.Deserialize(reader);
			reader.Close();

            // Create FMOD stream for audio if available
            info.audio = false;
            if (gota != 0)
            {
                if (PlaybackCodec.fmod_init(data) == 0)
                    if (PlaybackCodec.fmod_create_stream(data, data, (uint)_export.Resolution) == 0)
                        info.audio = true;
            }

            info.channels = LoadExportInfo();
			info.data = data;
			info.chdata = new byte[info.channels];
			info.type = Info.Types.Video;
			Initialise(info.channels);
		}

		private static void Initialise(int channels)
		{
			_update = 0;
			_cmd = new ICommand[channels];
			for (int i = 0; i != channels; i++)
				_cmd[i] = new _8BitCommand(0);
			if (_progress == null)
				_progress = new Stopwatch();
			ReadFrame();
			_updateRate.Reset();
			_progress.Reset();
			_frame = 0;
			foreach (IMediaModuleInstance media in _media)
			    media.LoadMedia(_progress.Elapsed);
		}

		public static void Load(string fileName)
        {
			_info.type = Info.Types.Invalid;

            // Instrumentation values
            if (_playbackTime == null)
            {
                _updateRate = new RefreshRateValue("Data dump playback update");
                VixenSystem.Instrumentation.AddValue(_updateRate);
                _playbackTime = new TimeValue("Data dump playback");
                VixenSystem.Instrumentation.AddValue(_playbackTime);
            }

			if (!PlaybackCodec.initialised) {
				PlaybackCodec.codec_init();
				PlaybackCodec.initialised = true;
			}

            if (_media == null)
                _media = new List<IMediaModuleInstance>();

            if (IsLoaded)
                Unload();

            if (fileName == null)
				return;
			CodecClose(ref _info);
			if (Path.GetExtension(fileName) == ".xml")
				LoadSequence(ref _info, fileName);
			else
				LoadVideo(ref _info, fileName);
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
			CodecClose(ref _info);
			if (_info.type == Info.Types.Video)
				_info.type = Info.Types.Invalid;
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

		private static void CodecClose(ref Info info)
		{
			if (info.data != IntPtr.Zero) {
				PlaybackCodec.fmod_close(info.data);
				PlaybackCodec.decode_close(info.data);
				PlaybackCodec.codec_free(info.data);
				info.data = IntPtr.Zero;
			}
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

		private static void ReadAudioFrame()
		{
			while (_info.audio && _nextUpdateTime + (long)_export.Resolution >= _info.audioTime) {
				int got = 0, video = 0;
				IntPtr pkt = PlaybackCodec.decode_read_packet(_info.data, out got, out video);
                if (got == 0)
                    break;
				if (video == 0) {
					IntPtr frame = PlaybackCodec.decode_audio_frame(_info.data, pkt);
					PlaybackCodec.fmod_queue_frame(_info.data, frame);
					_info.audioTime += PlaybackCodec.decode_audio_frame_length(frame);
				} else
					PlaybackCodec.decode_free_packet(pkt);
			}
		}

        private static void ReadFrame()
        {
			if (_info.type == Info.Types.Sequence) {
				UInt16 channels = ReadHeader();
				var data = _dataIn.ReadBytes(channels);
				for (int i = 0; i != channels; i++)
					((_8BitCommand)_cmd[i]).CommandValue = data[i];
				ReadAudioFrame();
			} else if (_info.type == Info.Types.Video) {
				IntPtr frame = PlaybackCodec.decode_video_frame(_info.data, ReadVideoPacket());
				if (frame != IntPtr.Zero) {
					IntPtr a = PlaybackCodec.decode_channels(_info.data, frame, _info.channels);
					Marshal.Copy(a, _info.chdata, 0, _info.channels);
					for (int i = 0; i != _info.channels; i++)
						((_8BitCommand)_cmd[i]).CommandValue = _info.chdata[i];
				}
			} else
				throw new Exception("Wrong state");
			_playbackTime.Set((double)(_frame * _export.Resolution) / 1000.0);
			_frame++;
            _updateRate.Increment();
        }

		private static void SkipFrame()
		{
			if (_info.type == Info.Types.Sequence) {
				UInt16 channels = ReadHeader();
				_dataIn.ReadBytes(channels);
				ReadAudioFrame();
			} else if (_info.type == Info.Types.Video) {
				PlaybackCodec.decode_free_packet(ReadVideoPacket());
			} else
				throw new Exception("Wrong state");
			_frame++;
		}

		private static IntPtr ReadVideoPacket()
		{
			for (;;) {
				int got = 0, video = 0;
				IntPtr pkt = PlaybackCodec.decode_read_packet(_info.data, out got, out video);
				if (got == 0)
					throw new Exception("End of file");
				if (video == 0) {
					IntPtr frame = PlaybackCodec.decode_audio_frame(_info.data, pkt);
					PlaybackCodec.fmod_queue_frame(_info.data, frame);
					continue;
				}
				return pkt;
			}
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

		private static void _ThreadFunc()
		{
			if (_info.audio)
				PlaybackCodec.fmod_play(_info.data);
			_nextUpdateTime = (long)_export.Resolution;
			_progress.Restart();
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
					Logging.Warn("Playback stopping: ", e.Message);
					break;
				}
				if (_info.data != IntPtr.Zero)
					PlaybackCodec.fmod_update(_info.data);
				_nextUpdateTime += (long)_export.Resolution;
			}
		}
    }
}
