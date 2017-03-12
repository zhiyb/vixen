using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Common.Controls.Scaling;
using Common.Controls.Theme;
using Common.Resources;
using Common.Resources.Properties;
using Vixen.Module.Editor;
using Vixen.Module.SequenceType;
using Vixen.Services;
using Vixen.Sys;

namespace VixenModules.App.Shows
{
	public partial class PlaybackTypeEditor : TypeEditorBase
	{
		public static ShowItem _showItem;
		public static Label ContolLabel1;
		public static Label ContolLabelSequence;
		public static TextBox ContolTextBoxSequence;
		public static Button ContolButtonSelectSequence;

		public PlaybackTypeEditor(ShowItem showItem)
		{
			InitializeComponent();

			ForeColor = ThemeColorTable.ForeColor;
			BackColor = ThemeColorTable.BackgroundColor;
			int iconSize = (int)(16 * ScalingTools.GetScaleFactor());
			ContolLabel1 = label1;
			ContolLabelSequence = labelSequence;
			ContolTextBoxSequence = textBoxSequence;
			ContolButtonSelectSequence = buttonSelectSequence;
			
			buttonSelectSequence.Image = Tools.GetIcon(Resources.folder_explore, iconSize);
			buttonSelectSequence.Text = "";
			ThemeUpdateControls.UpdateControls(this);
			_showItem = showItem;
		}

		private void textBoxSequence_TextChanged(object sender, EventArgs e)
		{
			_showItem.Playback_FileName = (sender as TextBox).Text;
			textBoxSequence.Text = _showItem.Playback_FileName;
			if (System.IO.File.Exists(_showItem.Playback_FileName))
			{
				labelSequence.Text = System.IO.Path.GetFileName(_showItem.Playback_FileName);
				_showItem.Name = "Run playback: " + System.IO.Path.GetFileName(_showItem.Playback_FileName);
			}
			else
			{
				labelSequence.Text = "(playback file not found)";
				_showItem.Name = labelSequence.Text;
			}
			FireChanged(_showItem.Name);
		}

		private void buttonSelectSequence_Click(object sender, EventArgs e)
		{
			openFileDialog.InitialDirectory = SequenceService.SequenceDirectory;

			// configure the open file dialog with a filter for currently available sequence types
			string filter = "Playback Network XML (*.xml)|*.xml|";
			string allTypes = "*.xml;";
			filter += "All files (*.*)|*.*";
			filter = "All Playback File Types (" + allTypes + ")|" + allTypes + "|" + filter;

			openFileDialog.Filter = filter;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxSequence.Text = openFileDialog.FileName;
			}
		}

		private void PlaybackTypeEditor_Load(object sender, EventArgs e)
		{
			textBoxSequence.Text = _showItem.Playback_FileName;
		}

	}
}
