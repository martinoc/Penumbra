using System;
using System.Globalization;
using System.Windows.Forms;
using PU_Bridge;
using PU_Keyboard;
using Penumbra.Classes;
using Penumbra.Windows;

namespace Penumbra
{

	static class Program
	{

#region Consts

		private const int MIN_FILTER_LEVEL = 0;
		private const int MAX_FILTER_LEVEL = 100;
        private const int MIN_HUE_LEVEL = 0;
        private const int MAX_HUE_LEVEL = 128;
        #endregion

        #region Variables

        public static INI m_INI = new INI("settings.ini");

		public static ContextMenu m_ContextMenu;
		public static NotifyIcon m_NotifyIcon;

		public static Hotkey m_HotkeyIncrease;
		public static Hotkey m_HotkeyDecrease;
		public static Hotkey m_HotkeyToggle;
		public static Hotkey m_HotkeySettings;
		public static Hotkey m_HotkeyQuit;

		private static SettingsWindow m_SettingsWindow;

#endregion

#region Properties

		public static bool Filtering
		{

			get { return m_INI.ReadBoolean(@"Filter", @"active"); }

			set { m_INI.WriteValue(@"Filter", @"active", (value ? "1" : "0")); }
            //this will cause problems if the folder is readOnly
		}
        public static bool Hueing
        {

            get { return m_INI.ReadBoolean(@"Hue", @"Hactive"); }

            set { m_INI.WriteValue(@"Hue", @"Hactive", (value ? "1" : "0")); }
            //this will cause problems if the folder is readOnly
        }
        public static int FilterLevel
		{

			get { return m_INI.ReadInteger(@"Filter", @"level"); }

		}

        public static int RedHueLevel
        {

            get { return m_INI.ReadInteger(@"Hue", @"RHlevel"); }

        }
        public static int GreenHueLevel
        {

            get { return m_INI.ReadInteger(@"Hue", @"GHlevel"); }

        }
        public static int BlueHueLevel
        {

            get { return m_INI.ReadInteger(@"Hue", @"BHlevel"); }

        }

        #endregion

        #region Ctor

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
		static void Main()
		{

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Bridge.Initialize();

			Application.ApplicationExit += Application_Exit;

			m_ContextMenu =
				new ContextMenu(new[]
									{
										new MenuItem(@"Show Settings", (p_Sender, p_Args) => ToggleSettings()) { Checked = false }, 
										new MenuItem(@"-"),
										new MenuItem(@"Quit", (p_Sender, p_Args) => Application.Exit())
									});
			

			m_NotifyIcon = new NotifyIcon
							   {
								   Icon = (Filtering ? Properties.Resources.eye_on : Properties.Resources.eye_off),
								   Visible = true,
								   ContextMenu = m_ContextMenu,
								   Text = "Penumbra - " + (Filtering ? @"On (" + FilterLevel + @"%)" : "Off")
							   };
			m_NotifyIcon.DoubleClick += m_NotifyIcon_DoubleClick;

			// Set and configure hotkeys
			//
			m_HotkeyIncrease = new Hotkey(m_INI.ReadString(@"Hotkeys", "increase")) { Enabled = Filtering };
			m_HotkeyIncrease.Pressed += delegate { IncreaseFilterLevel(); };

			m_HotkeyDecrease = new Hotkey(m_INI.ReadString(@"Hotkeys", "decrease")) { Enabled = Filtering };
			m_HotkeyDecrease.Pressed += delegate { DecreaseFilterLevel(); };

			m_HotkeyToggle = new Hotkey(m_INI.ReadString(@"Hotkeys", "toggle"));
			m_HotkeyToggle.Pressed += delegate { ToggleFilter(); };

			m_HotkeySettings = new Hotkey(m_INI.ReadString(@"Hotkeys", "settings"));
			m_HotkeySettings.Pressed += delegate { ToggleSettings(); };

			m_HotkeyQuit = new Hotkey(m_INI.ReadString(@"Hotkeys", "quit"));
			m_HotkeyQuit.Pressed += (p_Sender, p_Args) => Application.Exit();

			m_HotkeyIncrease.Register();
			m_HotkeyDecrease.Register();
			m_HotkeyToggle.Register();
			m_HotkeySettings.Register();
			m_HotkeyQuit.Register();

			// Check the initial filter state and act according it
			//
			if (Filtering)
				Filter.SetBrightness(FilterLevelToBrightness(byte.Parse(FilterLevel.ToString(CultureInfo.InvariantCulture))),Hueing,RedHueLevel,GreenHueLevel,BlueHueLevel);

            Monitors.Instance.Init();

			Application.Run();

		}

        #endregion

        #region Public Functions

        public static void ToggleHue()
        {
            Hueing = !Hueing;

            if (Filtering)
            {
                Filter.SetBrightness(FilterLevelToBrightness(FilterLevel), Hueing, RedHueLevel, GreenHueLevel, BlueHueLevel);
            }
        }
        public static void ToggleFilter()
		{

			if (Filtering)
			{

				m_NotifyIcon.Icon = Properties.Resources.eye_off;

				Filter.ResetBrightness();

				m_HotkeyIncrease.Enabled = m_HotkeyDecrease.Enabled = false;

				if (m_SettingsWindow != null)
				{

					m_SettingsWindow.InternalChangeInProgress = true;
                    m_SettingsWindow.cb_Filter.Checked = m_SettingsWindow.tb_FilterLevel.Enabled = false;
                    m_SettingsWindow.lb_FilterLevel.Enabled = false;
                    m_SettingsWindow.tb_FilterLevel.Enabled = false;

                    m_SettingsWindow.InternalChangeInProgress = false;

				}

				m_NotifyIcon.Text = @"Penumbra - Off";

			}
			else
			{

				m_NotifyIcon.Icon = Properties.Resources.eye_on;

				Filter.SetBrightness(FilterLevelToBrightness(byte.Parse(m_INI.ReadString(@"Filter", @"level"))), m_INI.ReadBoolean(@"Hue", @"Hactive"), m_INI.ReadInteger(@"Hue", @"RHlevel"), m_INI.ReadInteger(@"Hue", @"GHlevel"), m_INI.ReadInteger(@"Hue", @"BHlevel"));

				m_HotkeyIncrease.Enabled = m_HotkeyDecrease.Enabled = true;

				if (m_SettingsWindow != null)
				{

					m_SettingsWindow.InternalChangeInProgress = true;
					m_SettingsWindow.cb_Filter.Checked = m_SettingsWindow.tb_FilterLevel.Enabled = true;
                    m_SettingsWindow.lb_FilterLevel.Enabled = true;
                    m_SettingsWindow.tb_FilterLevel.Enabled = true;
                    m_SettingsWindow.InternalChangeInProgress = false;

				}

				m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"%)";

			}

            Filtering = !Filtering;

		}

        public static void CloseWindow()
        {
            m_ContextMenu.MenuItems[0].Text = @"Show Settings";

            m_SettingsWindow = null;
        }


		public static int BrightnessToFilterLevel(byte p_Brightness)
		{

			return Math.Abs(p_Brightness - 110);

		}

		public static byte FilterLevelToBrightness(int p_FilterLevel)
		{

			return (byte) (Math.Abs(p_FilterLevel - 100) + 10);

		}
        public static void SetRedHue(int p_RedHueLevel)
        {
            if (p_RedHueLevel < MIN_HUE_LEVEL)
                p_RedHueLevel = MIN_HUE_LEVEL;
            else if (p_RedHueLevel > MAX_HUE_LEVEL)
                p_RedHueLevel = MAX_HUE_LEVEL;

            Filter.SetBrightness(FilterLevelToBrightness(FilterLevel),Hueing, p_RedHueLevel, GreenHueLevel,BlueHueLevel);

            if (m_SettingsWindow != null)
                m_SettingsWindow.nud_RedHue.Value = p_RedHueLevel;

            m_INI.WriteValue(@"Hue", @"RHlevel", p_RedHueLevel.ToString(CultureInfo.InvariantCulture));

            if (Hueing)
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"% {R" + RedHueLevel + "G" + GreenHueLevel + "B" + BlueHueLevel + "})";
            }
            else
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"%)";

            }

        }
        public static void SetGreenHue(int p_GreenHueLevel)
        {
            if (p_GreenHueLevel < MIN_HUE_LEVEL)
                p_GreenHueLevel = MIN_HUE_LEVEL;
            else if (p_GreenHueLevel > MAX_HUE_LEVEL)
                p_GreenHueLevel = MAX_HUE_LEVEL;

            Filter.SetBrightness(FilterLevelToBrightness(FilterLevel), Hueing, RedHueLevel, p_GreenHueLevel, BlueHueLevel);

            if (m_SettingsWindow != null)
                m_SettingsWindow.nud_GreenHue.Value = p_GreenHueLevel;

            m_INI.WriteValue(@"Hue", @"GHlevel", p_GreenHueLevel.ToString(CultureInfo.InvariantCulture));

            if (Hueing)
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"% {R" + RedHueLevel + "G" + p_GreenHueLevel + "B" + BlueHueLevel + "})";
            }
            else
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"%)";

            }


        }
        public static void SetBlueHue(int p_BlueHueLevel)
        {
            if (p_BlueHueLevel < MIN_HUE_LEVEL)
                p_BlueHueLevel = MIN_HUE_LEVEL;
            else if (p_BlueHueLevel > MAX_HUE_LEVEL)
                p_BlueHueLevel = MAX_HUE_LEVEL;

            Filter.SetBrightness(FilterLevelToBrightness(FilterLevel), Hueing, RedHueLevel, GreenHueLevel, p_BlueHueLevel);

            if (m_SettingsWindow != null)
                m_SettingsWindow.nud_BlueHue.Value = p_BlueHueLevel;

            m_INI.WriteValue(@"Hue", @"BHlevel", p_BlueHueLevel.ToString(CultureInfo.InvariantCulture));

            if (Hueing)
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"% {R" + RedHueLevel + "G" + GreenHueLevel + "B" + BlueHueLevel + "})";
            }
            else
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"%)";

            }


        }
        public static void SetBrightness(int p_FilterLevel)
		{

			if (p_FilterLevel < MIN_FILTER_LEVEL)
				p_FilterLevel = MIN_FILTER_LEVEL;
			else if (p_FilterLevel > MAX_FILTER_LEVEL)
				p_FilterLevel = MAX_FILTER_LEVEL;

			Filter.SetBrightness(FilterLevelToBrightness(p_FilterLevel), Hueing, RedHueLevel, GreenHueLevel, BlueHueLevel);

			if (m_SettingsWindow != null)
				m_SettingsWindow.tb_FilterLevel.Value = p_FilterLevel;

            m_INI.WriteValue(@"Filter", @"level", p_FilterLevel.ToString(CultureInfo.InvariantCulture));

            if (Hueing)
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"% {R" + RedHueLevel + "G" + GreenHueLevel + "B" + BlueHueLevel + "})";
            }
            else
            {
                m_NotifyIcon.Text = @"Penumbra - On (" + FilterLevel + @"%)";

            }

        }

#endregion

#region Private Functions

		private static void IncreaseFilterLevel()
		{

			SetBrightness(FilterLevel + 10);

		}

		private static void DecreaseFilterLevel()
		{

			SetBrightness(FilterLevel - 10);

		}

		private static void ToggleSettings(bool p_Dispose = false)
		{

			if (!p_Dispose && m_SettingsWindow == null)
			{

				m_ContextMenu.MenuItems[0].Text = @"Hide Settings";
				
				m_SettingsWindow = new SettingsWindow();
				m_SettingsWindow.Show();

			}
			else
			{

				m_ContextMenu.MenuItems[0].Text = @"Show Settings";

				if (m_SettingsWindow != null)
				{

					m_SettingsWindow.Hide();
					m_SettingsWindow.Dispose();
					m_SettingsWindow = null;

				}

			}

		}

#endregion

#region Class Functions

		private static void Application_Exit(object sender, EventArgs e)
		{

			ToggleSettings(true);

			m_ContextMenu = null;

			m_NotifyIcon.Visible = false;
			m_NotifyIcon = null;

			// Restore the old brightness
			Filter.Finalize();

		}

// ReSharper disable InconsistentNaming
		private static void m_NotifyIcon_DoubleClick(object sender, EventArgs e)
// ReSharper restore InconsistentNaming
		{
			
			ToggleFilter();

		}
		
#endregion

	}

}
