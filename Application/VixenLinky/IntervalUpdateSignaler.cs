using System.Diagnostics;
using System.Threading;

namespace Vixen.Sys.Output
{
	internal class IntervalUpdateSignaler : IOutputDeviceUpdateSignaler
	{
		private readonly Stopwatch _stopwatch;
		private long _nextUpdateTime;

		public IntervalUpdateSignaler()
		{
			_stopwatch = Stopwatch.StartNew();
			_nextUpdateTime = 0;
		}

		public EventWaitHandle UpdateSignal { private get; set; }

		public int UpdateInterval { get; set; }

		public void RaiseSignal()
		{
			if (_nextUpdateTime == 0)
				_nextUpdateTime = _stopwatch.ElapsedMilliseconds + UpdateInterval;

			_Sleep(_nextUpdateTime - _stopwatch.ElapsedMilliseconds);
			_nextUpdateTime += UpdateInterval;
		}

		private void _Sleep(long timeInMs)
		{
			if (timeInMs > 1) {
				Thread.Sleep((int) timeInMs);
			}
		}
	}
}