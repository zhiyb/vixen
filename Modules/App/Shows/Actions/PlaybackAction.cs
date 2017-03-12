using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vixen.Execution;
using Vixen.Execution.Context;
using Vixen.Module.Media;
using Vixen.Sys;
using Vixen.Services;
using VixenModules.Sequence.Timed;

namespace VixenModules.App.Shows
{
	public class PlaybackAction : Action
	{
		private static readonly NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();
		
		public PlaybackAction(ShowItem showItem)
			: base(showItem)
        {
        }

		public override void Execute()
		{
			try
			{
                base.Execute();
                Playback.Load(ShowItem.Playback_FileName);
                Playback.PlaybackEnded += playback_Ended;
                Playback.Start();
			}
			catch (Exception ex)
			{
			    Logging.Error("Could not execute playback " + ShowItem.Playback_FileName + "; " + ex.Message);
			}
		}

        private void playback_Ended(object sender, EventArgs e)
        {
            Playback.PlaybackEnded -= playback_Ended;
            base.Complete();
        }

        public override void Stop()
        {
            Playback.PlaybackEnded -= playback_Ended;
            Playback.Stop();
            base.Stop();
		}
        
		protected override void Dispose(bool disposing)
		{
			if (disposing)
                Playback.Unload();
            Playback.PlaybackEnded -= playback_Ended;
            base.Dispose(disposing);
		}
	}
}