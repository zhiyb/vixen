namespace VixenModules.App.Playback
{
	using System;
	using Vixen.Module.App;
	using Vixen.Sys;

	public class PlaybackModule : AppModuleInstanceBase
	{
		private const string ID_MENU = "Playback_Main";
		private IApplication _application;
		private PlaybackForm _form;

		public override IApplication Application
		{
			set { _application = value; }
		}

		public override void Loading()
		{
			InitializeForm();
			_AddMenu();
		}

		public override void Unloading()
		{
			if (_form != null) {
				_form.Dispose();
				_form = null;
			}

			_RemoveMenu();
		}

		private void InitializeForm()
		{
			_form = new PlaybackForm();
			_form.Closed += _form_Closed;
		}

		private void OnMainMenuOnClick(object sender, EventArgs e)
		{
			if (_form == null) {
				InitializeForm();
			}

			_form.Show();
		}

		private void _AddMenu()
		{
			if (_application != null
			    && _application.AppCommands != null) {
				AppCommand toolsMenu = _application.AppCommands.Find("Tools");
				if (toolsMenu == null)
				{
					toolsMenu = new AppCommand("Tools", "Tools");
					_application.AppCommands.Add(toolsMenu);
				}
				var myMenu = new AppCommand(ID_MENU, "Playback");
				myMenu.Click += OnMainMenuOnClick;
				toolsMenu.Add(myMenu);
			}
		}

		private void _RemoveMenu()
		{
			if (_application != null
			    && _application.AppCommands != null) {
				_application.AppCommands.Remove(ID_MENU);
			}
		}

		private void _form_Closed(object sender, EventArgs e)
		{
			_form.Dispose();
			_form = null;
		}
	}
}