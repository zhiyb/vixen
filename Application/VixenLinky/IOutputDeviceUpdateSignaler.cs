using System.Threading;

namespace Vixen.Sys.Output
{
	public interface IOutputDeviceUpdateSignaler
	{
		EventWaitHandle UpdateSignal { set; }
		int UpdateInterval { get; set; }
		void RaiseSignal();
	}
}