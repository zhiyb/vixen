using Vixen.Instrumentation;
using Vixen.Sys.Output;

namespace Vixen.Sys.Instrumentation
{
	internal class RefreshRateValue : RateValue
	{
		public RefreshRateValue(string name)
			: base(string.Format("{0} refresh rate", name))
		{
		}

		protected override string _GetFormattedValue()
		{
			return _GetValue().ToString("0.00 Hz");
		}
	}
}