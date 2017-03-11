using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Controls;
using Common.Controls.Theme;
using Common.Resources.Properties;
using Vixen.Sys;
using Vixen.Sys.Instrumentation;

namespace VixenModules.App.Playback
{
    public partial class PlaybackForm : BaseForm
    {
        public PlaybackForm()
        {
            InitializeComponent();
            ForeColor = ThemeColorTable.ForeColor;
            BackColor = ThemeColorTable.BackgroundColor;
            ThemeUpdateControls.UpdateControls(this);
            Icon = Resources.Icon_Vixen3;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            Vixen.Sys.Playback.Load(tbFile.Text);
            Vixen.Sys.Playback.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Vixen.Sys.Playback.Stop();
        }

        private void PlaybackForm_Load(object sender, EventArgs e)
        {
            tUpdate.Start();
        }

        private void tUpdate_Tick(object sender, EventArgs e)
        {
            lStatus.Text = Vixen.Sys.Playback.StatusString;
        }
    }
}
