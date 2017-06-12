using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vixen.Sys;
using Vixen.Module;
using Vixen.Module.Controller;
using Vixen.Commands;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace VixenModules.Output.TCPLinky
{
    internal class TCPLinky : ControllerModuleInstanceBase
    {
        private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();
        private byte[] _lastValues;
        private TCPLinkyData _data;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Stopwatch _timeoutStopwatch;
        private int _outputCount;

        public static byte HEADER_1 = 0xDE;
        public static byte HEADER_2 = 0xAD;
        public static byte HEADER_3 = 0xBE;
        public static byte HEADER_4 = 0xEF;

		public static byte COMMAND_SET_VALUES = 0x01;

		// 4 bytes header, 1 byte command (set frame), 1 byte stream
		public static byte[] header = {HEADER_1, HEADER_2, HEADER_3, HEADER_4, COMMAND_SET_VALUES, 0};

        public TCPLinky()
        {
            Logging.Trace("Constructor()");
            _data = new TCPLinkyData();
            _timeoutStopwatch = new Stopwatch();
            DataPolicyFactory = new DataPolicyFactory();
        }

        private void _setupDataBuffers()
        {
            Logging.Trace(LogTag + "_setupDataBuffers()");
			_lastValues = new byte[_outputCount];
        }

        public override int OutputCount
        {
            get { return _outputCount; }
            set
            {
                _outputCount = value;
                _setupDataBuffers();
            }
        }

        public override IModuleDataModel ModuleData
        {
            get { return _data; }
            set
            {
                _data = value as TCPLinkyData;
                CloseConnection();
            }
        }

        public override bool HasSetup
        {
            get { return true; }
        }

        public override bool Setup()
        {
            Logging.Trace(LogTag + "Setup()");
            TCPLinkySetup setup = new TCPLinkySetup(_data);
            if (setup.ShowDialog() == DialogResult.OK) {
                if (setup.Address != null)
                    _data.Address = setup.Address;
                _data.Port = setup.Port;
                _data.Stream = setup.Stream;
                CloseConnection();
                return true;
            }

            return false;
        }

        private bool FakingIt()
        {
            return _data.Address.ToString().Equals("0.0.0.0");
        }

        private string HostInfo()
        {
            return _data.Address + ":" + _data.Port + ":" + _data.Stream;
        }

        private string LogTag
        {
            get { return "[" + HostInfo() + "]: "; }
        }


        private bool OpenConnection()
        {
            Logging.Trace(LogTag + "OpenConnection()");

            // start off closing the connection
            CloseConnection();

            if (_data.Address == null) {
                Logging.Warn(LogTag + "Trying to connect with a null IP address.");
                return false;
            }

            if (FakingIt())
                return true;

            try {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(_data.Address, _data.Port);
            } catch (Exception ex) {
                Logging.Warn(LogTag + "TCPLinky: Failed connect to host", ex);
                return false;
            }

            try {
                _networkStream = _tcpClient.GetStream();
                Logging.Debug(LogTag + "New connection to  host");
                Logging.Debug(LogTag + "(WriteTimeout default is " + _networkStream.WriteTimeout + ")");
            } catch (Exception ex) {
                Logging.Warn(LogTag + "Failed stream for host", ex);
                _tcpClient.Close();
                return false;
            }

            // reset the last values. That means that *any* values that come in will be 'new', and be sent out.
            _setupDataBuffers();

            _timeoutStopwatch.Reset();
            _timeoutStopwatch.Start();
            
            return true;
        }

        private void CloseConnection()
        {
            Logging.Trace(LogTag + "CloseConnection()");

            if (FakingIt())
                return;

            if (_networkStream != null) {
                Logging.Trace(LogTag + "Closing network stream...");
                _networkStream.Close();
                Logging.Trace(LogTag + "Network stream closed.");
                _networkStream = null;
            }

            if (_tcpClient != null) {
                Logging.Trace(LogTag + "Closing TCP client...");
                _tcpClient.Close();
                Logging.Trace(LogTag + "TCP client closed.");
                _tcpClient = null;
            }
            
            _timeoutStopwatch.Reset();
        }

        public override void Start()
        {
            Logging.Trace(LogTag + "Start()");
            _setupDataBuffers();
            base.Start();
        }

        public override void Stop()
        {
            Logging.Trace(LogTag + "Stop()");
            base.Stop();
        }

        public override void Pause()
        {
            Logging.Trace(LogTag + "Pause()");
            base.Pause();
        }

        public override void Resume()
        {
            Logging.Trace(LogTag + "Resume()");
            base.Resume();
        }

        public override void UpdateState(int chainIndex, ICommand[] outputStates)
        {
            if (_networkStream == null && !FakingIt()) {
                bool success = OpenConnection();
                if (!success) {
                    Logging.Warn(LogTag + "failed to connect to device, not updating state");
                    return;
                }
            }

			// build up transmission packet
            byte[] data = new byte[4 + 1 + 2 + outputStates.Length * 3];
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

            //data[totalPacketLength++] = (byte)_data.Stream;
            int lengthPos = totalPacketLength;
			int totalChannels = 0;
			totalPacketLength += 2;

            bool changed = false;
			int channels = outputStates.Length;
			for (int i = 0; i < channels; i++) {
				if (outputStates[i] == null)
					continue;
				byte newValue = ((_8BitCommand)outputStates[i]).CommandValue;
                if (_lastValues[i] != newValue) {
					changed = true;
                    data[totalPacketLength++] = (byte)i;
                    data[totalPacketLength++] = (byte)(i >> 8);
                    data[totalPacketLength++] = newValue;
                    _lastValues[i] = newValue;
                    totalChannels++;
                }
            }

			data[lengthPos] = (byte)totalChannels;
			data[lengthPos + 1] = (byte)(totalChannels >> 8);

            // don't bother writing anything if we haven't acutally *changed* any values...
            // (also, send at least a 'null' update command every 10 seconds. I think there's a bug in the micro
            // firmware; it doesn't seem to close network connections properly. Need to diagnose more, later.)
            if (true || changed || _timeoutStopwatch.ElapsedMilliseconds >= 10000) {
                try {
                    _timeoutStopwatch.Restart();
                    if (FakingIt()) {
                        System.Threading.Thread.Sleep(1);
                    } else {
                        _networkStream.Write(data, 0, totalPacketLength);
                        _networkStream.Flush();
                    }
                } catch (Exception ex) {
                    Logging.Warn(LogTag + "failed to write data to device during update", ex);
                    CloseConnection();
                }
            }
        }
    }
}