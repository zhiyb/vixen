﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Vixen.Module;

namespace VixenModules.App.WebServer
{
	[DataContract]
	public class Data : ModuleDataModelBase
	{
		public Data()
			: base()
		{
			//Default the Webserver port to 8080
			HttpPort = 8080;
		}
		public override IModuleDataModel Clone()
		{
			return MemberwiseClone() as IModuleDataModel;
		}

		[DataMember]
		public int HttpPort { get; set; }
	}

}
