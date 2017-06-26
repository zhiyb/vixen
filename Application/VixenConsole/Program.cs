using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vixen;
using Vixen.Sys;
using Vixen.Sys.Output;
using Vixen.Module;
using Vixen.Module.Controller;
using Vixen.Services;

namespace VixenConsole
{
	internal static class Program
	{
		private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();

		private const string NOT_PROVIDED = "(Not Provided)";
		private static ManualResetEvent _stopSignal;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			_stopSignal = new ManualResetEvent(false);
			Playback.PlaybackEnded += PlaybackEnded;

			Profiler prof = null;
			bool uPlayback = false, uController = false;

			var e = args.GetEnumerator();
			while (e.MoveNext()) {
				switch (e.Current.ToString()) {
				case "-p":
					if (!e.MoveNext())
						break;
					long itvl;
					if (!long.TryParse(e.Current.ToString(), out itvl))
						break;
					Logging.Info("Starting profiler with interval " + itvl + "ms ...");
					prof = new Profiler(itvl);
					prof.Start();
					break;
				case "-u":
					if (!e.MoveNext())
						break;
					switch (e.Current.ToString()) {
					case "playback":
						uPlayback = true;
						break;
					case "controller":
						uController = true;
						break;
					}
					break;
				case "tidy":
					VixenSystem.LoadMinimal();
					Logging.Info("Saving configrations...");
					var task = VixenSystem.SaveSystemAndModuleConfigAsync();
					task.Wait();
					if (task.Result)
						Logging.Info("Configuration saved");
					break;
				case "controller":
					if (!e.MoveNext())
						break;
					switch (e.Current.ToString()) {
					case "list":
						VixenSystem.LoadMinimal();
						_ListControllerModules();
						break;
					case "config":
						VixenSystem.LoadMinimal();
						_ListModuleConfigs();
						break;
					}
					break;
				case "start":
					VixenSystem.LoadMinimal();
					if (uController)
						foreach (var c in VixenSystem.OutputControllers)
							c.UpdateInterval = 0;
					Execution.OpenExecution();
					while (e.MoveNext()) {
						Playback.Load(e.Current.ToString());
						if (uPlayback)
							Playback.Test();
						else
							Playback.Start();
						if (Playback.IsRunning) {
							_stopSignal.WaitOne();
							_stopSignal.Reset();
						}
					}
					Execution.CloseExecution();
					break;
				}
			}

			if (prof != null) {
				prof.Stop();
				prof.Log();
			}
		}

		private static void PlaybackEnded(object sender, EventArgs e)
		{
			_stopSignal.Set();
		}

		private static void _ListModuleConfigs()
		{
			Console.Write("Controller configurations:\n");
			foreach (var dev in VixenSystem.OutputDeviceManagement.Devices) {
				var controller = VixenSystem.OutputControllers.GetController(dev.Id);
				Console.Write("\n" + dev.Name + "\n");
				Console.Write("\tID:       " + dev.Id + "\n");
				Console.Write("\tChannels: " + controller.Outputs.Length + "\n");
				Console.Write("\tInterval: " + dev.UpdateInterval + "\n");
				IModuleDescriptor descriptor = ApplicationServices.GetModuleDescriptor(dev.ModuleId);
				Console.Write("\tHardware: " + descriptor.TypeName + "\n");
				Console.Write("\t\tID: " + descriptor.TypeId + "\n");
				Type type = descriptor.ModuleDataClass;
				if (type != null) {
					var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					var data = controller.ModuleData;
					foreach (var p in prop) {
						var v = p.GetGetMethod().Invoke(data, null);
						Console.Write("\t\t" + p.Name + ":\t" + (v ?? "(null)") + "\n");
					}
				}
			}
		}

		private static void _ListControllerModules()
		{
			Console.Write("Available controllers:\n");
			IModuleDescriptor[] descriptors = ApplicationServices.GetModuleDescriptors("Controller");
			foreach (IModuleDescriptor descriptor in descriptors) {
				Console.Write("\n" + descriptor.TypeName + "\n");
				Console.Write("\tID:          " + descriptor.TypeId + "\n");
				Console.Write("\tAuthor:      " + _GetModuleAuthor(descriptor) + "\n");
				Console.Write("\tVersion:     " + _GetModuleVersion(descriptor) + "\n");
				Console.Write("\tDescription: " + _GetModuleDescription(descriptor) + "\n");
				Type type = descriptor.ModuleDataClass;
				if (type != null) {
					Console.Write("\tData members:\n");
					var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					var data = ApplicationServices.Get<IControllerModuleInstance>(descriptor.TypeId).ModuleData;
					foreach (var p in prop) {
						var v = p.GetGetMethod().Invoke(data, null);
						Console.Write("\t\t" + p.PropertyType.Name + "\t" + p.Name + " (" + (v ?? "null") + ")\n");
					}
				}
			}
		}

		private static string _GetModuleDescription(IModuleDescriptor descriptor)
		{
			try {
				return descriptor.Description;
			}
			catch {
				return NOT_PROVIDED;
			}
		}

		private static string _GetModuleAuthor(IModuleDescriptor descriptor)
		{
			try {
				return descriptor.Author;
			}
			catch {
				return NOT_PROVIDED;
			}
		}

		private static string _GetModuleVersion(IModuleDescriptor descriptor)
		{
			try {
				return descriptor.Version;
			}
			catch {
				return NOT_PROVIDED;
			}
		}
	}
}
