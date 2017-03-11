using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vixen.Module.App;

namespace VixenModules.App.Playback
{
	public class PlaybackDescriptor : AppModuleDescriptorBase
	{
		private Guid _typeId = new Guid("{A34A18C5-D39F-40F8-A38B-822C5D0C40BD}");

		public override string TypeName
		{
			get { return "Playback control"; }
		}

		public override Guid TypeId
		{
			get { return _typeId; }
		}

		public override string Author
		{
			get { return "Yubo Zhi"; }
		}

		public override string Description
		{
			get { return "Playback controller output dumps"; }
		}

		public override string Version
		{
			get { return "0.1"; }
		}

		public override Type ModuleClass
		{
			get { return typeof (PlaybackModule); }
		}
	}
}