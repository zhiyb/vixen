using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Vixen.Sys.Output;

namespace Vixen
{
	public class TCPLinky
	{
		private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		private IOutputDeviceUpdateSignaler _updateSignaler;
		private AutoResetEvent _updateSignalerSync;
		private EventWaitHandle _finished;

		private Thread _thread;
		private bool _stop;

		private TcpClient _tcpClient;
		private NetworkStream _networkStream;

		private IPAddress _host;
		private int _port;

		private int _start, _channels;
		private byte[] _lastValues;
		private bool _first;

		private const int STOP_TIMEOUT = 4000; // Four seconds should be plenty of time for a thread to stop.

		public TCPLinky(string host, int port, int interval, int start, int channels)
		{
			_thread = new Thread(_ThreadFunc);
			_stop = true;
			_updateSignalerSync = new AutoResetEvent(false);
			_updateSignaler = _CreateUpdateSignaler();
			_updateSignaler.UpdateInterval = interval;
			_finished = new EventWaitHandle(false, EventResetMode.ManualReset);

			IPAddress.TryParse(host, out _host);
			_port = port;
			log.Info("Host " + host + ":" + port + ", interval " + interval);

			_start = start - 1;
			_channels = channels;
			log.Info("Start " + start + ", channels " + channels);
		}

		private string HostInfo()
		{
			return _host + ":" + _port;
		}

		private string LogTag
		{
			get { return "[" + HostInfo() + "]: "; }
		}

		private bool OpenConnection()
		{
			log.Trace(LogTag + "OpenConnection()");

			// start off closing the connection
			CloseConnection();

			if (_host == null) {
				log.Warn(LogTag + "Trying to connect with a null IP address.");
				return false;
			}

			try {
				_tcpClient = new TcpClient();
				_tcpClient.Connect(_host, _port);
			} catch (Exception ex) {
				log.Warn(ex, LogTag + "TCPLinky: Failed connect to host");
				return false;
			}

			try {
				_networkStream = _tcpClient.GetStream();
				log.Debug(LogTag + "New connection to  host");
				log.Debug(LogTag + "(WriteTimeout default is " + _networkStream.WriteTimeout + ")");
			} catch (Exception ex) {
				log.Warn(ex, LogTag + "Failed stream for host");
				_tcpClient.Close();
				return false;
			}

			// reset the last values. That means that *any* values that come in will be 'new', and be sent out.
			_lastValues = new byte[_channels];
			return true;
		}

		private void CloseConnection()
		{
			log.Trace(LogTag + "CloseConnection()");

			if (_networkStream != null) {
				log.Trace(LogTag + "Closing network stream...");
				_networkStream.Close();
				log.Trace(LogTag + "Network stream closed.");
				_networkStream = null;
			}

			if (_tcpClient != null) {
				log.Trace(LogTag + "Closing TCP client...");
				_tcpClient.Close();
				log.Trace(LogTag + "TCP client closed.");
				_tcpClient = null;
			}
		}

		// 4 bytes header, 1 byte command (set values), 1 byte stream
		private static byte[] header = { 0xde, 0xad, 0xbe, 0xef, 0x01, 0x00 };

		private void UpdateState()
		{
			if (_networkStream == null) {
				bool success = OpenConnection();
				if (!success) {
					log.Warn(LogTag + "failed to connect to device, not updating state");
					return;
				}
			}

			byte[] values = new byte[_channels];
			Array.Copy(VixenLinky.Program.Data, _start, values, 0, _channels);

			// build up transmission packet
			byte[] data = new byte[4 + 1 + 2 + _channels * 3];
			int totalPacketLength = 0;

			// protocol is:	4 bytes header
			//				1 byte command
			//
			// at the moment, only the 'Set Values' blinky-protocol command is supported.
			// Set Values:	1 byte for the stream (ie. 0-2, for which RS485 stream)
			//				2 bytes for the number of channels following (LOW byte first)
			//				3 bytes (repeated): channel (LOW byte first), value.

			Array.Copy(header, data, header.Length);
			totalPacketLength += header.Length;

			int lengthPosL = totalPacketLength++;
			int lengthPosH = totalPacketLength++;
			int totalChannels = 0;

			bool changed = false;
			for (int i = 0; i < _channels; i++) {
				byte newValue = values[i];
				if (_lastValues[i] != newValue) {
					changed = true;
					data[totalPacketLength++] = (byte)(i & 0xff);
					data[totalPacketLength++] = (byte)((i >> 8) & 0xff);
					data[totalPacketLength++] = newValue;
					_lastValues[i] = newValue;
					totalChannels++;
				}
			}

			//int totalData = totalPacketLength - lengthPosL - 1;
			data[lengthPosL] = (byte)(totalChannels & 0xFF);
			data[lengthPosH] = (byte)((totalChannels >> 8) & 0xFF);

			// don't bother writing anything if we haven't acutally *changed* any values...
			if (changed || _first || true) {
				_first = false;
				try {
					_networkStream.Write(data, 0, totalPacketLength);
					_networkStream.Flush();
				} catch (Exception ex) {
					log.Warn(ex, LogTag + "failed to write data to device during update");
					CloseConnection();
				}
			}
		}

		private void _ThreadFunc()
		{
			try {
				while (!_stop) {
					UpdateState();

					// Wait for the next go 'round
					_WaitOnSignal(_updateSignaler);
				}
			} catch (Exception e) {
				log.Fatal(e);
			}

			_stop = true;
			_finished.Set();
		}

		private void _WaitOnSignal(IOutputDeviceUpdateSignaler signaler)
		{
			//long timeBeforeSignal = _localTime.ElapsedMilliseconds;

			signaler.RaiseSignal();
			//_updateSignalerSync.WaitOne();

			//long timeAfterSignal = _localTime.ElapsedMilliseconds;
			//_sleepTimeActualValue.Set(timeAfterSignal - timeBeforeSignal);
		}

		private IOutputDeviceUpdateSignaler _CreateUpdateSignaler()
		{
			IOutputDeviceUpdateSignaler signaler = new IntervalUpdateSignaler();
			signaler.UpdateInterval = 1000;
			signaler.UpdateSignal = _updateSignalerSync;

			return signaler;
		}

		public void Start()
		{
			_stop = false;
			_first = true;
			_thread.Start();
		}

		public void Stop()
		{
			_stop = true;
		}

		public void WaitForFinish()
		{
			if (!_finished.WaitOne(STOP_TIMEOUT)) {
				// Timed out waiting for a stop.
				//(This will prevent hangs in stopping, due to controller code failing to stop).
				throw new TimeoutException(string.Format("Controller {0} failed to stop in the required amount of time.", "TCPLinky"));
			}
		}
	}
}
