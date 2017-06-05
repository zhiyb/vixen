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
			VixenSystem.LoadMinimal();
			Type t = Type.GetType("VixenModules.Output.TCPLinky.TCPLinkyData, TCPLinky");
			Logging.Fatal("Type " + (t == null ? "null" : "exists"));
			Playback.PlaybackEnded += PlaybackEnded;
			if (args.Length >= 1) {
				switch (args[0]) {
				case "tidy":
					Logging.Info("Saving configrations...");
					var task = VixenSystem.SaveSystemAndModuleConfigAsync();
					task.Wait();
					if (task.Result)
						Logging.Info("Configuration saved");
					break;
				case "controller":
					if (args.Length >= 2) {
						switch (args[1]) {
						case "list":
							_ListControllerModules();
							break;
						case "config":
							_ListModuleConfigs();
							break;
						}
					}
					break;
				case "start":
					if (args.Length >= 2) {
						Execution.OpenExecution();
						for (int i = 1; i < args.Length; i++) {
							Playback.Load(args[i]);
							Playback.Start();
							if (Playback.IsRunning) {
								_stopSignal.WaitOne();
								_stopSignal.Reset();
							}
						}
						Execution.CloseExecution();
					}
					break;
				}
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
