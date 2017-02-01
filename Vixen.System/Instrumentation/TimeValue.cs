using Vixen.Instrumentation;

namespace Vixen.Sys.Instrumentation
{
	public class TimeValue : InstrumentationValue
	{
		private long cnt=1;
		private double time=0;

		public TimeValue( string name)
			: base(name)
		{
		}

		/**/
		public void Set(double value)
		{
            time = value;
			cnt++;
		}

		protected override double _GetValue()
		{
            return time;
		}
		/**/

		protected override string _GetFormattedValue()
		{
			return string.Format("time {0} s,  cnt {1}", time, cnt);
		}

		public override void Reset()
		{
            time = 0;
			cnt=1;
		}
	}
}