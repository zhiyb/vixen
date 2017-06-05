namespace Vixen.Sys.State.Execution.Behavior
{
	internal class StandardClosingBehavior
	{
		public static void Run()
		{
			Sys.Execution.Shutdown();

			WindowsMultimedia wm = new WindowsMultimedia();
			wm.EndEnhancedResolution();

			// Release all contexts.
			if (VixenSystem.Contexts != null)
				VixenSystem.Contexts.ReleaseContexts();

			// Stop all output devices.
			VixenSystem.OutputDeviceManagement.StopAll();
		}
	}
}