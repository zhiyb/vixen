﻿using System;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VixenModules.SequenceType.Vixen2x
{
    public class Vixen2SequenceData
    {
        protected internal string FileName { get; private set; }

        protected internal string ProfileName { get; private set; }

		protected internal string ProfilePath { get; private set; }

        protected internal int EventPeriod { get; private set; }

        protected internal byte[] EventData { get; private set; }

        protected internal string SongFileName { get; private set; }

		protected internal string SongPath { get; private set; }

        protected internal int SeqLengthInMills { get; private set; }

        protected internal int TotalEventsCount { get; private set; }

        protected internal int ElementCount { get; private set; }

        protected internal int EventsPerElement { get; private set; }

        protected internal List<ChannelMapping> mappings { get; private set; }

        protected internal Vixen2SequenceData(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("Cannot Locate " + fileName);
            }
            FileName = fileName;
            mappings = new List<ChannelMapping>();
            ParseFile();
        }

		private void ParseFile()
		{
			XElement root = null;
			using (FileStream stream = new FileStream(FileName, FileMode.Open))
			{
				root = XElement.Load(stream);
			}
			foreach (XElement element in root.Descendants())
			{
				switch (element.Name.ToString())
				{
					case "Time":
						SeqLengthInMills = Int32.Parse(element.Value);
						break;
					case "EventPeriodInMilliseconds":
						EventPeriod = Int32.Parse(element.Value);
						break;
					case "EventValues":
						EventData = Convert.FromBase64String(element.Value);
						break;
					case "Audio":
						SongFileName = element.Attribute("filename").Value;
						break;
					case "Profile":
						ProfileName = element.Value;
						break;
					// This node will exist if we have a flattend profile so load the channel information
					case "Channel":
						if (element.FirstAttribute.Name.LocalName.Equals("name"))
						{
							CreateMappingList(element, 2);
						}
						else if (element.FirstAttribute.Name.LocalName.Equals("number"))
						{
							//just break out of hear cause this is in the older verison of vixen
							break;
						}
						else
						{
							CreateMappingList(element, 1);
						}
						
						break;
					default:
						//ignore
						break;
				}
			}

			if (!String.IsNullOrEmpty(SongFileName))
				MessageBox.Show(String.Format("Audio File {0} is associated with this sequence, please select the location of the audio file.", SongFileName), "Select Audio Location", MessageBoxButtons.OK, MessageBoxIcon.Information);
			var dialog = new FolderBrowserDialog();
			using (dialog)
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					SongPath = dialog.SelectedPath + "\\";
				}
			}

			//check to see if the ProfileName is not null, if it isn't lets notify the user so they
			//can let us load the data
			if (!String.IsNullOrEmpty(ProfileName))
			{
				MessageBox.Show(String.Format("Vixen {0}.pro is associated with this sequence, please select the location of the profile.", ProfileName), "Select Profile Location", MessageBoxButtons.OK, MessageBoxIcon.Information);
				dialog = new FolderBrowserDialog();
				
				using (dialog)
				{
					if (dialog.ShowDialog() == DialogResult.OK)
					{
						ProfilePath = dialog.SelectedPath;
						root = null;
						using (FileStream stream = new FileStream(String.Format(@"{0}\{1}.pro", ProfilePath, ProfileName), FileMode.Open))
						{
							root = XElement.Load(stream);
						}
						foreach (XElement element in root.Descendants())
						{
							switch (element.Name.ToString())
							{
								case "Channel":
									//This exists in the 2.5.x versions of Vixen
									//<Channel name="Mini Tree Red 1" color="-65536" output="0" id="5576725746726704001" enabled="True" />
									if (element.FirstAttribute.Name.LocalName.Equals("name"))
									{
										CreateMappingList(element,2);
									}
									else if (element.FirstAttribute.Name.LocalName.Equals("number"))
									{
										//just break out of hear cause this is in the older verison of vixen
										break;
									}
									//This is exists in the older versions
									//<Channel color="-262330" output="0" id="633580705216250000" enabled="True">FenceIcicles-1</Channel>
									else
									{
										CreateMappingList(element, 1);
									}
								
									break;
								default:
									//ignore
									break;
							}
						}

					}
				}

				// These calculations could have been put in the properties, but then it gets confusing to debug because of all the jumping around.
				TotalEventsCount = Convert.ToInt32(Math.Ceiling((double)(SeqLengthInMills / EventPeriod))); ;
				ElementCount = EventData.Length / TotalEventsCount;
				EventsPerElement = EventData.Length / ElementCount;

				//if the profile name is null or empty then the sequence must have been flattened so indicate that.
				if (String.IsNullOrEmpty(ProfileName))
				{
					ProfileName = "Sequence has been flattened no profile is available";
				}
			}
		}


		private void CreateMappingList(XElement element, int version)
		{
			//if version == 1 then we have an old profile that we are dealing with so we have
 			//to get the node value for the channel name
			if (version == 1)
			{
				mappings.Add(new ChannelMapping( element.FirstNode.ToString(),
						  Color.FromArgb(int.Parse(element.Attribute("color").Value)),
						  (mappings.Count + 1).ToString(),
						  element.Attribute("output").Value));
			}
				//must be version 2.5 so get the channel name from attribute 'name'
			else
			{
				mappings.Add(new ChannelMapping(element.Attribute("name").Value,
										  Color.FromArgb(int.Parse(element.Attribute("color").Value)),
										  (mappings.Count + 1).ToString(),
										  element.Attribute("output").Value));

			}
		}
    }
}
