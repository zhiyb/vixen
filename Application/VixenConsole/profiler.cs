using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Vixen.Sys;
using Vixen.Instrumentation;
using Vixen.Sys.Instrumentation;

namespace VixenConsole
{
	public class Profiler
	{
		private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();

		private static Thread _thread;
		private bool _stop;
		private long _itvl;
		private Stopwatch _sw;

		private struct file_t {
			public string name, path;
			public StreamReader s;
			public StreamWriter w;
		};
		private List<file_t> _files;

		private struct record_t {
			public long time;
			public List<string> files;
			public List<Tuple<string, double> > values;
		};
		private List<record_t> _records;

		public Profiler(long interval)
		{
			_itvl = interval;
			_sw = new Stopwatch();
			_files = new List<file_t>();
			_records = new List<record_t>();

			var pid = Process.GetCurrentProcess().Id;
			AddFile("stat", "/proc/" + pid + "/stat");
			AddFile("io", "/proc/" + pid + "/io");
			AddFile("pstat", "/proc/stat");
		}

		private void AddFile(string name, string path)
		{
			file_t f = new file_t();
			f.name = name;
			f.path = path;
			try {
				f.s = new StreamReader(f.path);
				f.w = new StreamWriter("prof/" + f.name + ".log");
			} catch (Exception e) {
				Logging.Warn(e, "Error opening " + f.path + " for profiling:");
				return;
			}
			_files.Add(f);
		}

		public void Start()
		{
			_stop = false;
			_thread = new Thread(_ThreadFunc);
			_thread.Start();
		}

		public void Stop()
		{
			_stop = true;
			_thread.Join();
			_sw.Stop();
		}

		public void Log()
		{
			StreamWriter si = new StreamWriter("prof/instrumentation.log");
			ulong total_p = 0, utime_p = 0, stime_p = 0;
			foreach (var rec in _records) {
				var f = _files.GetEnumerator();
				ulong total = 0, utime = 0, stime = 0;
				foreach (var r in rec.files) {
					f.MoveNext();
					if (f.Current.name == "pstat") {
						string[] sf = r.Split(" ".ToCharArray(), 10);
						// Splitted fields: cpu, ' ', user, nice,..
						ulong user = ulong.Parse(sf[2]), nice = ulong.Parse(sf[3]);
						ulong sys = ulong.Parse(sf[4]), idle = ulong.Parse(sf[5]);
						ulong iowait = ulong.Parse(sf[6]), irq = ulong.Parse(sf[7]), sirq = ulong.Parse(sf[8]);
						total = user + nice + sys + idle + iowait + irq + sirq;
					} else if (f.Current.name == "stat") {
						string[] sf = r.Split(" ".ToCharArray(), 16);
						utime = ulong.Parse(sf[13]);
						stime = ulong.Parse(sf[14]);
					}
					f.Current.w.Write(r + "\n");
				}
				si.Write("@ " + rec.time + "\n");
				if (total_p != 0) {
					si.Write(((double)(utime - utime_p) / (double)(total - total_p)) + "\tUser mode CPU usage\n");
					si.Write(((double)(stime - stime_p) / (double)(total - total_p)) + "\tKernel mode CPU usage\n");
				}
				total_p = total;
				utime_p = utime;
				stime_p = stime;
				foreach (var v in rec.values)
					si.Write(v.Item2 + "\t" + v.Item1 + "\n");
				si.Write("\n");
			}
			si.Close();
			foreach (var f in _files)
				f.w.Close();
		}

		private void _ThreadFunc()
		{
			_sw.Restart();
			long sleep = _sw.ElapsedMilliseconds;
			while (!_stop) {
				record_t rec = new record_t();
				rec.files = new List<string>();
				ulong total = 0;
				ulong utime = 0, stime = 0;
				foreach (var f in _files) {
					f.s.BaseStream.Seek(0, SeekOrigin.Begin);
					rec.files.Add(f.s.ReadToEnd());
				}
				rec.values = new List<Tuple<string, double> >();
				if (VixenSystem.Instrumentation != null)
					foreach (var i in VixenSystem.Instrumentation.Values)
						rec.values.Add(new Tuple<string, double>(i.Name, i.Value));
				long ms = _sw.ElapsedMilliseconds;
				rec.time = ms;
				_records.Add(rec);
				sleep += _itvl;
				if (sleep - ms > 0)
					Thread.Sleep((int)(sleep - ms));
			}
		}
	}
}
