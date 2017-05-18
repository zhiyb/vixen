using System;
using System.IO;
using System.Threading;
using Vixen;
using Vixen.Sys.Output;

namespace VixenLinky
{
	internal static class Program
	{
		private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		private static IOutputDeviceUpdateSignaler _updateSignaler;
		private static AutoResetEvent _updateSignalerSync;
		private static byte[] _data = null;

		public static byte[] Data
		{
			get { return _data; }
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			_updateSignalerSync = new AutoResetEvent(false);
			_updateSignaler = _CreateOutputDeviceUpdateSignaler();
			if (args.Length != 7) {
				log.Fatal(new ArgumentException(), "Expected 7 arguments");
				Environment.Exit(1);
			}

			string file = args[0];
			_updateSignaler.UpdateInterval = Convert.ToInt32(args[1]);
			log.Info("File \"" + file + "\", interval " + _updateSignaler.UpdateInterval);

			BinaryReader dataIn = null;
			try {
				dataIn = Load(file);
				_data = ReadFrame(dataIn);
			} catch (Exception e) {
				log.Fatal(e);
				Environment.Exit(1);
			}

			TCPLinky controller = null;
			log.Info("Starting controller");
			try {
				string host = args[2];
				int port = Convert.ToInt32(args[3]);
				int interval = Convert.ToInt32(args[4]);

				int start = Convert.ToInt32(args[5]);
				int channels = Convert.ToInt32(args[6]);

				controller = new TCPLinky(host, port, interval, start, channels);
				controller.Start();
			} catch (Exception e) {
				log.Fatal(e);
				Environment.Exit(1);
			}

			log.Info("Starting main loop");
			// Thread main loop
			try {
				while (true) {
					//log.Info("Loop event");
					Array.Copy(ReadFrame(dataIn), _data, _data.Length);

					// Wait for the next go 'round
					_WaitOnSignal(_updateSignaler);
					//_WaitOnPause();
				}
			} catch (Exception e) {
				log.Fatal(e);
			}

			controller.Stop();
			controller.WaitForFinish();
		}

		private static BinaryReader Load(string path)
		{
			FileStream fs = File.OpenRead(path);
			BinaryReader rdr = new BinaryReader(fs);
			return rdr;
		}

		// 4 bytes header, 1 byte command (set frame), 1 byte stream
		private static byte[] header = { 0xde, 0xad, 0xbe, 0xef, 0x02, 0x00 };

		private static byte[] ReadFrame(BinaryReader rdr)
		{
			int i = 0;
			while (i != header.Length)
			{
				byte c = rdr.ReadByte();
				if (c != header[i++])
				{
					i = c == header[0] ? 1 : 0;
					log.Warn("Frame header error @" + rdr.BaseStream.Position);
				}
			}
			UInt16 channels = rdr.ReadUInt16();
			return rdr.ReadBytes(channels);
			//Array.Copy(data.ReadBytes(channels), _data, channels);
			//_playbackTime.Set((double)(_frame * _export.Resolution) / 1000.0);
			//_updateRate.Increment();
		}

		private static IOutputDeviceUpdateSignaler _CreateOutputDeviceUpdateSignaler()
		{
			IOutputDeviceUpdateSignaler signaler = new IntervalUpdateSignaler();
			signaler.UpdateInterval = 1000;
			signaler.UpdateSignal = _updateSignalerSync;

			return signaler;
		}

		private static void _WaitOnSignal(IOutputDeviceUpdateSignaler signaler)
		{
			//long timeBeforeSignal = _localTime.ElapsedMilliseconds;

			signaler.RaiseSignal();
			//_updateSignalerSync.WaitOne();

			//long timeAfterSignal = _localTime.ElapsedMilliseconds;
			//_sleepTimeActualValue.Set(timeAfterSignal - timeBeforeSignal);
		}
	}
}
