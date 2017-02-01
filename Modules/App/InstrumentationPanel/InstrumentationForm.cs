using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Common.Controls;
using Common.Controls.Theme;
using Common.Resources.Properties;
using Vixen.Sys;
using Vixen.Sys.Instrumentation;

namespace VixenModules.App.InstrumentationPanel
{
	public partial class InstrumentationForm : BaseForm
	{
        private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();
        private static FileStream _fs;
        private static UTF8Encoding _enc;
        private Stopwatch _localTime;
        private TimeValue _timeValue;

        public InstrumentationForm()
		{
			InitializeComponent();
			ForeColor = ThemeColorTable.ForeColor;
			BackColor = ThemeColorTable.BackgroundColor;
			textBox1.ForeColor = ThemeColorTable.ForeColor;
			textBox1.BackColor = ThemeColorTable.BackgroundColor;
			ThemeUpdateControls.UpdateControls(this);
			Icon = Resources.Icon_Vixen3;
            _enc = new UTF8Encoding(true);
            _localTime = new Stopwatch();
            _fs = null;
            _timeValue = new TimeValue("Instrumentation logging");
		}

        private void StartLogging()
        {
            if (_fs != null)
                return;
            _localTime.Restart();
            try
            {
                var path =
                    System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Vixen");
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
                var now = NLog.Time.TimeSource.Current.Time.ToString("yyyyMMdd_HHmmss");
                _fs = File.Open(path + "\\Instrumentation_" + now + ".log", FileMode.OpenOrCreate);
                VixenSystem.Instrumentation.AddValue(_timeValue);
            }
            catch (Exception e)
            {
                Logging.Warn(e, "Instrumentation Logging Disabled");
            }
        }

		private void InstrumentationForm_Load(object sender, EventArgs e)
        {
            StartLogging();
            timer.Start();
		}

        private void log(string[] str)
        {
            if (_fs == null)
                return;
            
            foreach (var s in str)
            {
                byte[] bytes = _enc.GetBytes(s + "\r\n");
                _fs.Write(bytes, 0, bytes.Length);
            }
        }

		private void timer_Tick(object sender, EventArgs e)
		{
            _timeValue.Set(_localTime.Elapsed.TotalSeconds);
			string[] lines = VixenSystem.Instrumentation.Values.Select(x => string.Format("{0}: {1}", x.Name , x.FormattedValue)).ToArray();
			textBox1.Lines = lines;
            log(lines);
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			foreach (var instrumentationValue in VixenSystem.Instrumentation.Values)
			{
				instrumentationValue.Reset();	
			}
			
		}
		private void buttonBackground_MouseHover(object sender, EventArgs e)
		{
			var btn = (Button)sender;
			btn.BackgroundImage = Resources.ButtonBackgroundImageHover;
		}

		private void buttonBackground_MouseLeave(object sender, EventArgs e)
		{
			var btn = (Button)sender;
			btn.BackgroundImage = Resources.ButtonBackgroundImage;

		}

        private void InstrumentationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _fs.Close();
            _fs = null;
            _localTime.Stop();
        }
    }
}