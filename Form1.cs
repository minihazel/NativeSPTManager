using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO.Compression;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System.Timers;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Management;
using Microsoft.VisualBasic.Devices;
using Microsoft.VisualBasic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Data.Common;
using System.Net.Sockets;
using System.Net;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Xml.Serialization;
using System.Threading;
using SharpCompress.Archives.Rar;
using Microsoft.VisualBasic.ApplicationServices;

namespace NativeSPTManager
{
    public partial class mainWindow : Form
    {
        public string curDir;
        public string configjson;
        public string documentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SPT Manager");
        public string documentsDisabledServerFolder;
        public string documentsDisabledClientFolder;
        private System.Windows.Forms.Timer startTimer;
        private int tickCount = 0;
        Process processToKill;
        Process launcherProcess;

        public bool serverBool = false;
        public bool isClientMod = false;
        public bool newModLoader = false;
        public bool darkTheme = false;

        public Color darkThemeBG = Color.FromArgb(255, 55, 55, 55);
        public Color darkThemeTextbox = Color.FromArgb(255, 75, 75, 75);
        public Color lightThemeBG = SystemColors.Control;

        public int clientModCount = 0;
        public List<string> fullPaths = new List<string>();

        public mainWindow()
        {
            InitializeComponent();

            documentsDisabledServerFolder = Path.Combine(documentsFolder, "Disabled Server Mods");
            documentsDisabledClientFolder = Path.Combine(documentsFolder, "Disabled Client Mods");

            if (!Directory.Exists(documentsFolder))
            {
                Directory.CreateDirectory(documentsFolder);
                Directory.CreateDirectory(documentsDisabledServerFolder);
                Directory.CreateDirectory(documentsDisabledClientFolder);

            } else if (!Directory.Exists(documentsDisabledServerFolder))
            {
                Directory.CreateDirectory(documentsDisabledServerFolder);
            } else if (!Directory.Exists(documentsDisabledClientFolder))
            {
                Directory.CreateDirectory(documentsDisabledClientFolder);
            }
            serverPath.Text = "None";
        }

        private void mainWindow_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.server_path == null || Properties.Settings.Default.server_path == "" || !Directory.Exists(Properties.Settings.Default.server_path))
            {
                if (MessageBox.Show($"It looks like {this.Text} has no server to connect to. Click Yes to browse for one, or No to exit.", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                    dialog.InitialDirectory = serverPath.Text;
                    dialog.IsFolderPicker = true;

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {

                        lblAppServerPath.Text = dialog.FileName;
                        serverPath.Text = Properties.Settings.Default.server_path;
                        Properties.Settings.Default.server_path = dialog.FileName;
                        Properties.Settings.Default.Save();

                        Application.Restart();

                    }
                } else
                {
                    Application.Exit();
                }
            } else
            {
                checkGameVersion();

                serverPath.Text = Properties.Settings.Default.server_path;
                curDir = Properties.Settings.Default.server_path;
                lblAppServerPath.Text = serverPath.Text;
                serverConfigTitle.Text = "";
                clientConfigTitle.Text = "";

                refreshUI();

                if (Properties.Settings.Default.profileEditing == true)
                {
                    lblAppProfileEditingToggle.Text = "Enabled";
                    lblAppProfileEditingToggle.ForeColor = Color.DodgerBlue;
                } else
                {
                    lblAppProfileEditingToggle.Text = "Disabled";
                    lblAppProfileEditingToggle.ForeColor = Color.IndianRed;
                }

                if (Properties.Settings.Default.dedicatedLauncher == true)
                {
                    lblAppLauncherToggle.Text = "Enabled";
                    lblAppLauncherToggle.ForeColor = Color.DodgerBlue;

                    btnLauncherStartProcess.Enabled = true;
                    // btnLauncherStartLauncher.Enabled = true;
                }
                else
                {
                    lblAppLauncherToggle.Text = "Disabled";
                    lblAppLauncherToggle.ForeColor = Color.IndianRed;

                    btnLauncherStartProcess.Enabled = false;
                    // btnLauncherStartLauncher.Enabled = false;
                }
            }
        }

        private void ToggleTheme(bool isDark)
        {
            if (isDark)
            {
                this.BackColor = darkThemeBG;
                this.ForeColor = Color.Gainsboro;
                topPanel.BackColor = darkThemeBG;
                topPanel.ForeColor = Color.Gainsboro;


                // Server mods
                panelServerMods.BackColor = darkThemeBG;
                panelServerMods.ForeColor = Color.Gainsboro;

                serverModsPanel.BackColor = darkThemeBG;
                serverModsPanel.ForeColor = Color.Gainsboro;

                serverOptionsPanel.BackColor = darkThemeBG;
                serverOptionsPanel.ForeColor = Color.Gainsboro;

                foreach (Control component in serverModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;

                        // btnServerSortUp.BackgroundImage = System.Drawing.Image.FromFile(Path.Combine(Environment.CurrentDirectory, "src\\Arrow-Up-DarkMode.png"));
                        // btnServerSortDown.BackgroundImage = System.Drawing.Image.FromFile(Path.Combine(Environment.CurrentDirectory, "src\\Arrow-Down-DarkMode.png"));

                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }

                foreach (Control component in serverOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 0;
                        btn.FlatStyle = FlatStyle.Flat;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        if (component.Name.ToLower() != "serverconfigbox")
                        {
                            component.BackColor = darkThemeTextbox;
                        }
                        else
                        {
                            component.BackColor = Color.FromArgb(255, 45, 45, 45);
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Client mods
                panelClientMods.BackColor = darkThemeBG;
                panelClientMods.ForeColor = Color.Gainsboro;

                clientModsPanel.BackColor = darkThemeBG;
                clientModsPanel.ForeColor = Color.Gainsboro;

                clientOptionsPanel.BackColor = darkThemeBG;
                clientOptionsPanel.ForeColor = Color.Gainsboro;

                foreach (Control component in clientModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }

                foreach (Control component in clientOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 0;
                        btn.FlatStyle = FlatStyle.Flat;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        if (component.Name.ToLower() != "clientconfigbox")
                        {
                            component.BackColor = darkThemeTextbox;
                        }
                        else
                        {
                            component.BackColor = Color.FromArgb(255, 45, 45, 45);
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }

                // Disabled mods
                panelDisabledMods.BackColor = darkThemeBG;
                panelDisabledMods.ForeColor = Color.Gainsboro;

                disabledModsPanel.BackColor = darkThemeBG;
                disabledModsPanel.ForeColor = Color.Gainsboro;

                foreach (Control component in disabledModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Profile editor
                panelProfile.BackColor = darkThemeBG;
                panelProfile.ForeColor = Color.Gainsboro;

                profileOptionsPanel.BackColor = darkThemeBG;
                profileOptionsPanel.ForeColor = Color.Gainsboro;

                profileHealthPanel.BackColor = darkThemeBG;
                profileHealthPanel.ForeColor = Color.Gainsboro;

                profileNotice.ForeColor = Color.Gainsboro;

                foreach (Control component in profileOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }

                foreach (Control component in profileHealthPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Settings
                panelSettings.BackColor = darkThemeBG;
                panelSettings.ForeColor = Color.Gainsboro;

                appSettingsPanel.BackColor = darkThemeBG;
                appSettingsPanel.ForeColor = Color.Gainsboro;

                appServerSettings.BackColor = darkThemeBG;
                appServerSettings.ForeColor = Color.Gainsboro;

                foreach (Control component in appSettingsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;

                        if (btn.Text.ToLower() == "reset")
                        {
                            btn.ForeColor = Color.IndianRed;
                        }

                        if (btn.Text.ToLower() == "browse")
                        {
                            btn.ForeColor = Color.DodgerBlue;
                        }

                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }

                foreach (Control component in appServerSettings.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = darkThemeTextbox;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeTextbox;

                        if (btn.Text.ToLower() == "btnappclearcache")
                        {
                            btn.ForeColor = Color.IndianRed;
                        }

                        if (btn.Text.ToLower() == "lblapplaunchertoggle")
                        {
                            btn.ForeColor = Color.DodgerBlue;
                        }

                        if (btn.Name.ToLower() == "lblappprofileeditingtoggle")
                        {
                            btn.ForeColor = Color.DodgerBlue;
                        }
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeTextbox;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Launcher
                panelLauncher.BackColor = darkThemeBG;
                panelLauncher.ForeColor = Color.Silver;

                panelRunOptions.BackColor = darkThemeBG;
                panelRunOptions.ForeColor = Color.Silver;

                foreach (Control component in panelLauncher.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = Color.Silver;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeBG;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = darkThemeBG;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeBG;
                            box.ForeColor = Color.Silver;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = darkThemeBG;
                        component.ForeColor = Color.Silver;
                    }
                }

                foreach (Control component in panelRunOptions.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = darkThemeBG;
                        btn.ForeColor = Color.Silver;
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = darkThemeBG;
                        component.ForeColor = Color.Silver;
                    }
                }

            }
            else
            {
                this.BackColor = lightThemeBG;
                this.ForeColor = SystemColors.ControlText;
                topPanel.BackColor = lightThemeBG;
                topPanel.ForeColor = SystemColors.ControlText;

                // Server mods
                panelServerMods.BackColor = lightThemeBG;
                panelServerMods.ForeColor = SystemColors.ControlText;

                serverModsPanel.BackColor = lightThemeBG;
                serverModsPanel.ForeColor = SystemColors.ControlText;

                serverOptionsPanel.BackColor = lightThemeBG;
                serverOptionsPanel.ForeColor = SystemColors.ControlText;

                serverModsPanel.BackColor = lightThemeBG;
                serverModsPanel.ForeColor = SystemColors.ControlText;

                foreach (Control component in serverModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatStyle = FlatStyle.Standard;

                        btn.BackColor = Color.Gainsboro;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = Color.Gainsboro;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = Color.Gainsboro;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }
                }

                foreach (Control component in serverOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatStyle = FlatStyle.Standard;

                        btn.BackColor = Color.Gainsboro;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        if (component.Name.ToLower() != "serverconfigbox")
                        {
                            component.BackColor = darkThemeTextbox;
                        } else
                        {
                            component.BackColor = Color.FromArgb(255, 45, 45, 45);
                        }

                        /*
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                        */
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Client mods
                panelClientMods.BackColor = lightThemeBG;
                panelClientMods.ForeColor = SystemColors.ControlText;

                clientModsPanel.BackColor = lightThemeBG;
                clientModsPanel.ForeColor = SystemColors.ControlText;

                clientOptionsPanel.BackColor = lightThemeBG;
                clientOptionsPanel.ForeColor = SystemColors.ControlText;

                foreach (Control component in clientModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatStyle = FlatStyle.Standard;

                        btn.BackColor = Color.Gainsboro;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = Color.Gainsboro;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = Color.Gainsboro;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }
                }

                foreach (Control component in clientOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatStyle = FlatStyle.Standard;

                        btn.BackColor = Color.Gainsboro;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        if (component.Name.ToLower() != "clientconfigbox")
                        {
                            component.BackColor = darkThemeTextbox;
                        }
                        else
                        {
                            component.BackColor = Color.FromArgb(255, 45, 45, 45);
                        }

                        /*
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = darkThemeTextbox;
                            box.ForeColor = Color.Silver;
                        }
                        */
                    }

                    if (component is ComboBox)
                    {
                        component.ForeColor = Color.Silver;
                        component.BackColor = darkThemeBG;
                    }
                }


                // Disabled mods
                panelDisabledMods.BackColor = lightThemeBG;
                panelDisabledMods.ForeColor = SystemColors.ControlText;

                disabledModsPanel.BackColor = lightThemeBG;
                disabledModsPanel.ForeColor = SystemColors.ControlText;

                foreach (Control component in disabledModsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatStyle = FlatStyle.Standard;

                        btn.BackColor = Color.Gainsboro;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = Color.Gainsboro;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = Color.Gainsboro;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }
                }


                // Profile editor
                panelProfile.BackColor = lightThemeBG;
                panelProfile.ForeColor = SystemColors.ControlText;

                profileOptionsPanel.BackColor = lightThemeBG;
                profileOptionsPanel.ForeColor = SystemColors.ControlText;

                profileHealthPanel.BackColor = lightThemeBG;
                profileHealthPanel.ForeColor = SystemColors.ControlText;

                profileNotice.ForeColor = Color.IndianRed;

                foreach (Control component in profileOptionsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = lightThemeBG;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = lightThemeBG;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }

                foreach (Control component in profileHealthPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }


                // Settings
                panelSettings.BackColor = lightThemeBG;
                panelSettings.ForeColor = SystemColors.ControlText;

                appSettingsPanel.BackColor = lightThemeBG;
                appSettingsPanel.ForeColor = SystemColors.ControlText;

                appServerSettings.BackColor = lightThemeBG;
                appServerSettings.ForeColor = SystemColors.ControlText;

                foreach (Control component in appSettingsPanel.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = lightThemeBG;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = lightThemeBG;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }

                foreach (Control component in appServerSettings.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        
                        if (btn.Text.ToLower() == "btnappclearcache")
                        {
                            btn.ForeColor = Color.IndianRed;
                        }

                        if (btn.Text.ToLower() == "lblapplaunchertoggle")
                        {
                            btn.ForeColor = Color.DodgerBlue;
                        }

                        if (btn.Name.ToLower() == "lblappprofileeditingtoggle")
                        {
                            btn.ForeColor = Color.DodgerBlue;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }


                // Launcher
                panelLauncher.BackColor = lightThemeBG;
                panelLauncher.ForeColor = SystemColors.ControlText;

                panelRunOptions.BackColor = lightThemeBG;
                panelRunOptions.ForeColor = SystemColors.ControlText;

                foreach (Control component in panelLauncher.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Panel && component.HasChildren)
                    {
                        component.BackColor = lightThemeBG;
                        foreach (TextBox box in component.Controls)
                        {
                            box.BackColor = lightThemeBG;
                            box.ForeColor = SystemColors.ControlText;
                        }
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }

                foreach (Control component in panelRunOptions.Controls)
                {
                    if (component is Label)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }

                    if (component is Button)
                    {
                        Button btn = (Button)component;
                        btn.FlatAppearance.BorderSize = 4;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = lightThemeBG;
                        btn.BackgroundImageLayout = ImageLayout.Zoom;

                        btn.BackColor = lightThemeBG;
                        btn.ForeColor = SystemColors.ControlText;
                    }

                    if (component is ComboBox)
                    {
                        component.BackColor = lightThemeBG;
                        component.ForeColor = SystemColors.ControlText;
                    }
                }
            }
        }

        private void checkGameVersion()
        {
            string orderFile = Path.Combine(Properties.Settings.Default.server_path, "Aki_Data\\Server\\configs\\core.json");
            string orderJSON = File.ReadAllText(orderFile);
            JObject order = JObject.Parse(orderJSON);

            this.Text = $"{this.Text} - {order["akiVersion"].ToString()} ({order["compatibleTarkovVersion"].ToString()})";

            if (order["compatibleTarkovVersion"].ToString().Contains("0.13"))
                checkOrderJSON();

        }

        private void checkOrderJSON()
        {
            string order = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");

            if (!File.Exists(order))
            {
                var jsonObject = new { order = new List<string>() };
                string json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                File.WriteAllText(order, json);
            }
            newModLoader = true;
        }

        private void updateOrder()
        {
            try
            {
                string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                string orderJSON = File.ReadAllText(orderFile);
                JObject order = JObject.Parse(orderJSON);
                
                string mods = $"{curDir}\\user\\mods";
                string[] modsFolder = Directory.GetDirectories(mods);

                foreach (string folder in modsFolder)
                {
                    string name = Path.GetFileName(folder);
                    bool exists = ((JArray)order["order"]).Any(t => t.Value<string>() == name);

                    if (!exists)
                        ((JArray)order["order"]).Add(name);
                }

                string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                File.WriteAllText(orderFile, output);
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        public void ListObjects(dynamic obj, ComboBox box)
        {
            foreach (var property in obj)
            {
                var propertyValue = property.Value;
                if (propertyValue == null) continue;
                if (propertyValue.GetType().IsPrimitive || propertyValue is string)
                {
                    box.Items.Add(propertyValue);
                }
                else
                {
                    if (propertyValue is JObject)
                    {
                        ListObjects((JObject)propertyValue, box);
                    } else
                    {
                        ListObjects(propertyValue, box);
                    }

                }

            }
        }

        private void loadServerMods()
        {
            serverDisplayMods.Items.Clear();
            if (this.Text.Contains("0.12") && newModLoader == false)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] serverFolders = Directory.GetDirectories(modsFolder);
                    foreach (string folder in serverFolders)
                    {
                        serverDisplayMods.Items.Add(Path.GetFileName(folder));
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }

            } else if (this.Text.Contains("0.13") && newModLoader == true)
            {
                try
                {
                    string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                    string orderJSON = File.ReadAllText(orderFile);
                    JObject order = JObject.Parse(orderJSON);
                    JArray array = ((JArray)order["order"]);

                    foreach (var item in array)
                    {
                        serverDisplayMods.Items.Add(item.ToString());
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }

            }

            
        }

        private void loadClientMods()
        {
            clientDisplayMods.Items.Clear();
            try
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                string[] clientFolders = Directory.GetDirectories(modsFolder);
                foreach (string folder in clientFolders)
                {
                    clientDisplayMods.Items.Add(Path.GetFileName(folder));
                }

                string[] clientFiles = Directory.GetFiles(modsFolder);
                foreach (string file in clientFiles)
                {
                    if (Path.GetFileName(file) != "aki-core.dll" &&
                        Path.GetFileName(file) != "aki-custom.dll" &&
                        Path.GetFileName(file) != "aki-debugging.dll" &&
                        Path.GetFileName(file) != "aki-singleplayer.dll" &&
                        Path.GetFileName(file).ToLower() != "configurationmanager.dll")
                    {
                        clientDisplayMods.Items.Add(Path.GetFileName(file));
                    }
                }
            } catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void loadDisabledMods()
        {
            disabledDisplayMods.Items.Clear();
            try
            {
                string[] clientfolders = Directory.GetDirectories(documentsDisabledClientFolder);
                string[] clientfiles = Directory.GetFiles(documentsDisabledClientFolder);
                string[] servermods = Directory.GetDirectories(documentsDisabledServerFolder);

                foreach (string mod in clientfiles)
                {
                    disabledDisplayMods.Items.Add(Path.GetFileName(mod));
                }

                foreach (string mod in clientfolders)
                {
                    disabledDisplayMods.Items.Add(Path.GetFileName(mod));
                }

                foreach (string mod in servermods)
                {
                    disabledDisplayMods.Items.Add(Path.GetFileName(mod));
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void loadProfiles()
        {
            try
            {
                string profilesFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\profiles");
                string[] profiles = Directory.GetFiles(profilesFolder);
                foreach (string profile in profiles)
                {
                    displayProfiles.Items.Add(Path.GetFileName(profile));
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void clearAllMods()
        {
            try
            {
                serverDisplayMods.Items.Clear();
                foreach (Control panel in serverModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }

                clientDisplayMods.Items.Clear();
                foreach (Control panel in clientModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }

                disabledDisplayMods.Items.Clear();
                foreach (Control panel in disabledModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }

                displayProfiles.Items.Clear();
                foreach (Control panel in profileOptionsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }

                foreach (Control panel in profileHealthPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }

            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void clearServerMods()
        {
            try
            {
                serverDisplayMods.Items.Clear();
                foreach (Control panel in serverModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void clearClientMods()
        {
            try
            {
                clientDisplayMods.Items.Clear();
                foreach (Control panel in clientModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void clearDisabledMods()
        {
            try
            {
                disabledDisplayMods.Items.Clear();
                foreach (Control panel in disabledModsPanel.Controls)
                {
                    foreach (Control txt in panel.Controls)
                    {
                        txt.Text = "";
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void enableServerButtons()
        {
            if (!newModLoader)
            {
                btnServerSortUp.Enabled = false;
                btnServerSortDown.Enabled = false;
                btnServerSortAlphabetical.Enabled = false;
            } else
            {
                btnServerSortUp.Enabled = true;
                btnServerSortDown.Enabled = true;
                btnServerSortAlphabetical.Enabled = true;
            }
            
            btnServerModDelete.Enabled = true;
            btnServerModDisable.Enabled = true;

            serverAuthorToggle.Enabled = true;
            serverVersionToggle.Enabled = true;
            serverSrcToggle.Enabled = true;
            serverAkiToggle.Enabled = true;
            serverNameToggle.Enabled = true;

            btnServerConfigValidate.Enabled = false;
            btnServerConfigOpen.Enabled = false;
            serverDisplayConfigs.Enabled = true;

            serverConfig.ReadOnly = true;

            serverOptionsPanel.Visible = true;
            lblServerConfigPlaceholder.Visible = false;
            serverConfigTitle.Visible = false;

        }

        private void enableClientButtons()
        {
            btnClientModDelete.Enabled = true;
            btnClientModDisable.Enabled = true;
            clientDisplayConfig.Enabled = true;
        }

        private void enableDisabledButtons()
        {
            btnDisabledModDelete.Enabled = true;
            btnDisabledModToggle.Enabled = true;
        }

        private void refreshUI()
        {
            if (newModLoader)
            {
                updateOrder();
                btnServerSortAlphabetical.Enabled = true;
            }

            clearAllMods();

            refreshServerUI();
            refreshClientUI();
            refreshDisabledUI();
            refreshProfileUI();
            loadProfiles();

            countMods();
        }

        private void resetUI()
        {
            // Server page
            btnServerSortUp.Enabled = false;
            btnServerSortDown.Enabled = false;
            btnServerSortAlphabetical.Enabled = false;
            btnServerModDelete.Enabled = false;
            btnServerModDisable.Enabled = false;

            btnServerConfigValidate.Enabled = false;
            btnServerConfigOpen.Enabled = false;
            btnServerConfigToggle.Enabled = false;
            btnServerConfigOpen.Enabled = false;
            btnServerConfigRestore.Enabled = false;

            serverDisplayConfigs.Enabled = false;
            serverConfig.ReadOnly = true;

            lblServerConfigPlaceholder.Visible = false;
            serverConfigTitle.Visible = false;

            // Client page
            btnClientModDelete.Enabled = false;
            btnClientModDisable.Enabled = false;
            clientDisplayConfig.Enabled = false;

            // Disabled page
            btnDisabledModDelete.Enabled = false;
            btnDisabledModToggle.Enabled = false;
        }

        private void refreshServerUI()
        {
            lblServerConfigPlaceholder.Visible = false;
            serverConfigTitle.Visible = false;
            serverConfig.ReadOnly = true;
            serverConfig.Text = "";
            serverDisplayConfigs.Items.Clear();
            serverDisplayConfigs.Enabled = false;


            loadServerMods();
        }

        private void refreshClientUI()
        {
            clientConfigPlaceholder.Visible = false;
            clientConfigTitle.Visible = false;
            lblClientModName.Text = "";
            lblClientModType.Text = "";

            clientConfig.ReadOnly = true;
            clientConfig.Text = "";
            clientDisplayConfig.Items.Clear();
            clientDisplayConfig.Enabled = false;

            btnClientModDisable.Enabled = false;
            btnClientModDelete.Enabled = false;
            btnClientConfigOpen.Enabled = false;
            btnClientConfigSave.Enabled = false;

            loadClientMods();
        }

        private void refreshDisabledUI()
        {
            disabledDisplayMods.Items.Clear();

            lblDisabledModName.Text = "";
            lblDisabledModType.Text = "";

            btnDisabledModToggle.Enabled = false;
            btnDisabledModDelete.Enabled = false;

            loadDisabledMods();
        }

        private void refreshProfileUI()
        {
            displayProfiles.Items.Clear();
            btnProfileDelete.Enabled = false;
        }

        private void readServerMod()
        {
            try
            {
                fullPaths.Clear();

                serverDisplayConfigs.Items.Clear();
                serverConfigTitle.Text = "";
                serverConfig.Text = "";
                btnServerConfigToggle.Enabled = false;

                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                string[] servermods = Directory.GetDirectories(modsFolder);
                for (int i = 0; i < servermods.Length; i++)
                {
                    if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                    {
                        enableServerButtons();

                        string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                        string packageFile = Path.Combine(selectedMod, "package.json");

                        string packagejson = File.ReadAllText(packageFile);
                        JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                        lblServerModAuthor.Text = json["author"].ToString();
                        lblServerModVersion.Text = json["version"].ToString();
                        lblServerModSrc.Text = json["main"].ToString();
                        lblServerModAkiVersion.Text = json["akiVersion"].ToString();
                        lblServerModName.Text = json["name"].ToString();

                        serverDisplayConfigs.Items.Clear();

                        readConfigFromServerMod(servermods[i]);

                        sortingTooltip.SetToolTip(btnServerSortUp, $"Move the load order up one slot on {serverDisplayMods.SelectedItem.ToString()}");
                        sortingTooltip.SetToolTip(btnServerSortDown, $"Move the load order down one slot on {serverDisplayMods.SelectedItem.ToString()}");
                    }
                }

            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void readConfigFromServerMod(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    string[] subdirs = Directory.GetDirectories(path);

                    if (Path.GetFileName(file).ToLower().Contains("cfg") && Path.GetExtension(file) == ".json")
                    {
                        string trimmedPath = file.Replace(path, "").TrimStart('\\');

                        serverDisplayConfigs.Items.Add(trimmedPath);
                        serverDisplayConfigs.Tag = $"cfg";

                        fullPaths.Add(file);

                    }
                    else if (Path.GetFileName(file).ToLower().Contains("config") && Path.GetExtension(file) == ".json")
                    {
                        string trimmedPath = file.Replace(path, "").TrimStart('\\');

                        serverDisplayConfigs.Items.Add(trimmedPath);
                        serverDisplayConfigs.Tag = $"config";

                        fullPaths.Add(file);

                    }
                }

                foreach (string subDirectory in Directory.GetDirectories(path))
                {
                    readConfigFromServerMod(subDirectory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access to the path '{0}' is denied.", path);
            }
        }

        private void readClientMod()
        {
            if (clientDisplayMods.SelectedItem.ToString().EndsWith(".dll"))
            {
                // Mod is a file
                try
                {
                    clientDisplayConfig.Items.Clear();
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");

                    string[] clientmods = Directory.GetFiles(modsFolder);
                    for (int i = 0; i < clientmods.Length; i++)
                    {
                        if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                        {
                            enableClientButtons();

                            btnClientModDisable.Enabled = true;
                            btnClientModDelete.Enabled = true;
                            lblClientModType.Text = "File based";
                            lblClientModName.Text = Path.GetFileNameWithoutExtension(clientmods[i]);

                            try
                            {
                                // Checking for config file
                                string selected = Path.GetFileNameWithoutExtension(clientmods[i]).ToString().ToLower();
                                string configsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\config");

                                string[] configs = Directory.GetFiles(configsFolder);
                                foreach (string config in configs)
                                {
                                    if (Path.GetFileNameWithoutExtension(config).ToLower().Contains(selected.ToLower()))
                                    {
                                        string activecfg = $"{configsFolder}\\{config}";
                                        clientDisplayConfig.Items.Add(Path.GetFileName(config));

                                        clientConfigTitle.Text = "";
                                        clientConfig.Text = "";

                                        clientOptionsPanel.Visible = true;
                                        clientConfig.ReadOnly = true;
                                        clientDisplayConfig.Enabled = true;

                                        btnClientConfigSave.Enabled = false;
                                        btnClientConfigOpen.Enabled = false;

                                        clientConfigPlaceholder.Visible = false;
                                        clientConfigTitle.Visible = false;

                                        break;
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
            else
            {
                // Mod is a folder
                try
                {
                    clientDisplayConfig.Items.Clear();
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                    string[] clientmods = Directory.GetDirectories(modsFolder);
                    for (int i = 0; i < clientmods.Length; i++)
                    {
                        if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                        {
                            btnClientModDisable.Enabled = true;
                            btnClientModDelete.Enabled = true;
                            lblClientModType.Text = "Folder based";
                            lblClientModName.Text = Path.GetFileName(clientmods[i]);

                            try
                            {
                                // Checking for config file
                                string selected = Path.GetFileNameWithoutExtension(clientmods[i]).ToString().ToLower();
                                string configsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\config");

                                string[] configs = Directory.GetFiles(configsFolder);
                                foreach (string config in configs)
                                {

                                    if (Path.GetFileNameWithoutExtension(config).ToLower().Contains(selected.ToLower()))
                                    {
                                        string activecfg = $"{configsFolder}\\{config}";
                                        clientDisplayConfig.Items.Add(Path.GetFileName(config));

                                        clientConfigTitle.Text = "";
                                        clientConfig.Text = "";

                                        clientOptionsPanel.Visible = true;
                                        clientConfig.ReadOnly = true;
                                        clientDisplayConfig.Enabled = true;

                                        btnClientConfigSave.Enabled = false;
                                        btnClientConfigOpen.Enabled = false;

                                        clientConfigPlaceholder.Visible = false;
                                        clientConfigTitle.Visible = false;

                                        break;
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void readDisabledMod()
        {
            try
            {
                if (disabledDisplayMods.SelectedItem.ToString().EndsWith(".dll"))
                {
                    // Mod is a client file
                    try
                    {
                        string[] clientmods = Directory.GetFiles(documentsDisabledClientFolder);
                        for (int i = 0; i < clientmods.Length; i++)
                        {
                            if (Path.GetFileName(clientmods[i]) == disabledDisplayMods.SelectedItem.ToString())
                            {
                                btnDisabledModToggle.Enabled = true;
                                btnDisabledModDelete.Enabled = true;
                                lblDisabledModType.Text = "Client mod (file based)";
                                lblDisabledModName.Text = Path.GetFileName(clientmods[i]);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine($"ERROR: {err}");
                        MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                    }

                }
                else
                {
                    // Mod is a folder
                    string modsFolder = Path.Combine(documentsDisabledServerFolder, disabledDisplayMods.Text);
                    string packageFile = Path.Combine(modsFolder, "package.json");

                    if (File.Exists(packageFile))
                    {
                        // Server folder
                        try
                        {
                            string[] servermods = Directory.GetDirectories(documentsDisabledServerFolder);
                            for (int i = 0; i < servermods.Length; i++)
                            {
                                if (Path.GetFileName(servermods[i]) == disabledDisplayMods.Text)
                                {
                                    btnDisabledModToggle.Enabled = true;
                                    btnDisabledModDelete.Enabled = true;
                                    lblDisabledModType.Text = "Server mod";
                                    lblDisabledModName.Text = Path.GetFileName(servermods[i]);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine($"ERROR: {err}");
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }

                    }
                    else
                    {
                        try
                        {
                            string[] clientmods = Directory.GetDirectories(documentsDisabledClientFolder);
                            for (int i = 0; i < clientmods.Length; i++)
                            {
                                if (Path.GetFileName(clientmods[i]) == disabledDisplayMods.SelectedItem.ToString())
                                {
                                    btnDisabledModToggle.Enabled = true;
                                    btnDisabledModDelete.Enabled = true;
                                    lblDisabledModType.Text = "Client mod (folder based)";
                                    lblDisabledModName.Text = Path.GetFileName(clientmods[i]);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine($"ERROR: {err}");
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }
                    }

                }

            } catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void readProfile()
        {
            try
            {
                string profilesFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\profiles");
                string profilesFile = Path.Combine(profilesFolder, displayProfiles.SelectedItem.ToString());

                string packagejson = File.ReadAllText(profilesFile);
                JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                string aid = json["characters"]["pmc"]["aid"].ToString();
                string profileUser = json["info"]["username"].ToString();
                string profileGameUser = json["characters"]["pmc"]["Info"]["Nickname"].ToString();
                string profilePass = json["info"]["password"].ToString();
                string profileEdition = json["characters"]["pmc"]["Info"]["GameVersion"].ToString();

                string headCur = json["characters"]["pmc"]["Health"]["BodyParts"]["Head"]["Health"]["Current"].ToString();
                string headMax = json["characters"]["pmc"]["Health"]["BodyParts"]["Head"]["Health"]["Maximum"].ToString();

                string leftArmCur = json["characters"]["pmc"]["Health"]["BodyParts"]["LeftArm"]["Health"]["Current"].ToString();
                string leftArmMax = json["characters"]["pmc"]["Health"]["BodyParts"]["LeftArm"]["Health"]["Maximum"].ToString();

                string RightArmCur = json["characters"]["pmc"]["Health"]["BodyParts"]["RightArm"]["Health"]["Current"].ToString();
                string RightArmMax = json["characters"]["pmc"]["Health"]["BodyParts"]["RightArm"]["Health"]["Maximum"].ToString();

                string ChestCur = json["characters"]["pmc"]["Health"]["BodyParts"]["Chest"]["Health"]["Current"].ToString();
                string ChestMax = json["characters"]["pmc"]["Health"]["BodyParts"]["Chest"]["Health"]["Maximum"].ToString();

                string StomachCur = json["characters"]["pmc"]["Health"]["BodyParts"]["Stomach"]["Health"]["Current"].ToString();
                string StomachMax = json["characters"]["pmc"]["Health"]["BodyParts"]["Stomach"]["Health"]["Maximum"].ToString();

                string LeftLegCur = json["characters"]["pmc"]["Health"]["BodyParts"]["LeftLeg"]["Health"]["Current"].ToString();
                string LeftLegMax = json["characters"]["pmc"]["Health"]["BodyParts"]["LeftLeg"]["Health"]["Maximum"].ToString();

                string RightLegCur = json["characters"]["pmc"]["Health"]["BodyParts"]["RightLeg"]["Health"]["Current"].ToString();
                string RightLegMax = json["characters"]["pmc"]["Health"]["BodyParts"]["RightLeg"]["Health"]["Maximum"].ToString();

                int totalHealth = Int32.Parse(headMax) +
                    Int32.Parse(leftArmMax) +
                    Int32.Parse(RightArmMax) +
                    Int32.Parse(ChestMax) +
                    Int32.Parse(StomachMax) +
                    Int32.Parse(LeftLegMax) +
                    Int32.Parse(RightLegMax);

                lblProfileCharacterId.Text = aid;
                lblProfileUsername.Text = profileUser;
                lblProfileGameUsername.Text = profileGameUser;
                lblProfilePassword.Text = profilePass;

                switch (profileEdition.ToLower())
                {
                    case "standard":
                        lblProfileEdition.Text = "Standard Edition";
                        break;

                    case "prepare_to_escape":
                        lblProfileEdition.Text = "Prepare to Escape";
                        break;

                    case "left_behind":
                        lblProfileEdition.Text = "Prepare to Escape";
                        break;

                    case "edge_of_darkness":
                        lblProfileEdition.Text = "Edge of Darkness";
                        break;
                }

                healthHead.Text = headCur;
                healthHeadMax.Text = headMax;

                healthLeftArm.Text = leftArmCur;
                healthLeftArmMax.Text = leftArmMax;

                healthRightArm.Text = RightArmCur;
                healthRightArmMax.Text = RightArmMax;

                healthChest.Text = ChestCur;
                healthChestMax.Text = ChestMax;

                healthStomach.Text = StomachCur;
                healthStomachMax.Text = StomachMax;

                healthLeftLeg.Text = LeftLegCur;
                healthLeftLegMax.Text = LeftLegMax;

                healthRightLeg.Text = RightLegCur;
                healthRightLegMax.Text = RightLegMax;

                healthTotal.Text = totalHealth.ToString();
                healthTotalMax.Text = totalHealth.ToString();

                btnProfileDelete.Enabled = true;

            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }

        }

        private void readProfile(string profileId)
        {
            try
            {
                string profilesFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\profiles");
                string profilesFile = Path.Combine(profilesFolder, profileId);

                string profileToRead = File.ReadAllText(profilesFile);

                JObject json = JsonConvert.DeserializeObject<JObject>(profileToRead);
                lblServerModAuthor.Text = json["author"].ToString();
                lblServerModVersion.Text = json["version"].ToString();
                lblServerModSrc.Text = json["main"].ToString();
                lblServerModAkiVersion.Text = json["akiVersion"].ToString();
                lblServerModName.Text = json["name"].ToString();


            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void countMods()
        {
            int i = 0;
            i += serverDisplayMods.Items.Count;
            i += clientDisplayMods.Items.Count;
            // i += disabledDisplayMods.Items.Count;

            counterTotalMods.Text = i.ToString();
            counterServerMods.Text = serverDisplayMods.Items.Count.ToString();
            counterClientMods.Text = clientDisplayMods.Items.Count.ToString();
            counterDisabledMods.Text = disabledDisplayMods.Items.Count.ToString();
        }

        void MoveItemUp(ref JArray order, string value)
        {
            int index = order.IndexOf(value);
            if (index <= 0)
                return;
            var element = order[index];
            order.RemoveAt(index);
            order.Insert(index - 1, element);
        }

        void MoveBoxItemUp(ComboBox box)
        {
            if (box.SelectedIndex < 0)
                return;
            var selectedItem = box.SelectedItem;
            int selectedIndex = box.SelectedIndex;
            box.Items.RemoveAt(selectedIndex);
            box.Items.Insert(selectedIndex - 1, selectedItem);

            box.SelectedIndex = selectedIndex - 1;
        }

        void MoveBoxItemDown(ComboBox box)
        {
            if (box.SelectedIndex < 0)
                return;
            var selectedItem = box.SelectedItem;
            int selectedIndex = box.SelectedIndex;
            box.Items.RemoveAt(selectedIndex);
            box.Items.Insert(selectedIndex + 1, selectedItem);

            box.SelectedIndex = selectedIndex + 1;
        }

        private void displayAlphabeticalSort()
        {
            serverDisplayLabel.Visible = true;
            serverDisplayLabel.Text = $"Sorted all mods alphabetically!";

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 2000; // 2 seconds
            timer.Elapsed += (tsender, b) =>
            {
                serverDisplayLabel.Invoke((MethodInvoker)delegate
                {
                    serverDisplayLabel.Visible = false;
                    serverDisplayLabel.Text = "{modName}";
                });
                timer.Stop();
            };
            timer.Start();
        }

        private void displayMoveSuccess(bool direction, string modName, int originalIndex, int newIndex)
        {
            int origin = originalIndex + 1;
            int newind = newIndex + 1;

            if (!direction)
            {
                serverDisplayLabel.Visible = true;
                serverDisplayLabel.Text = $"Moved {modName} down from index {origin} to {newind}!";

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 2000; // 2 seconds
                timer.Elapsed += (tsender, b) =>
                {
                    serverDisplayLabel.Invoke((MethodInvoker)delegate
                    {
                        serverDisplayLabel.Visible = false;
                        serverDisplayLabel.Text = "{modName}";
                    });
                    timer.Stop();
                };
                timer.Start();

            } else
            {
                serverDisplayLabel.Visible = true;
                serverDisplayLabel.Text = $"Moved {modName} up from index {origin} to {newind}!";

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 2000; // 2 seconds
                timer.Elapsed += (tsender, b) =>
                {
                    serverDisplayLabel.Invoke((MethodInvoker)delegate
                    {
                        serverDisplayLabel.Visible = false;
                        serverDisplayLabel.Text = "{modName}";
                    });
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void displayConfigSuccess(bool isClient)
        {
            if (!isClient)
            {
                serverConfigDisplayLabel.Visible = true;
                serverConfigDisplayLabel.Text = $"Saved!";

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 2000; // 2 seconds
                timer.Elapsed += (tsender, b) =>
                {
                    serverConfigDisplayLabel.Invoke((MethodInvoker)delegate
                    {
                        serverConfigDisplayLabel.Visible = false;
                        serverConfigDisplayLabel.Text = "{configName}";
                    });
                    timer.Stop();
                };
                timer.Start();
            } else
            {
                clientConfigDisplayLabel.Visible = true;
                clientConfigDisplayLabel.Text = $"Saved!";

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 2000; // 2 seconds
                timer.Elapsed += (tsender, b) =>
                {
                    clientConfigDisplayLabel.Invoke((MethodInvoker)delegate
                    {
                        clientConfigDisplayLabel.Visible = false;
                        clientConfigDisplayLabel.Text = "{configName}";
                    });
                    timer.Stop();
                };
                timer.Start();
            }
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }

            Directory.Delete(sourceDir, true);
        }

        private void StartTimer()
        {
            startTimer = new System.Windows.Forms.Timer();
            startTimer.Interval = 1000;
            startTimer.Tick += STimer_Tick;
            startTimer.Start();

            lblLauncherServerInfo.Visible = true;
            lblLauncherServerInfo.Text = "Server is starting!\nPlease wait until the server has connected, the tool may lag significantly.";
        }

        private void STimer_Tick(object sender, EventArgs e)
        {
            tickCount++;

            if (IsPortOpen(6969))
            {
                startTimer.Stop();

                string[] serverMods = { };
                string[] clientMods = { };

                foreach (string item in serverDisplayMods.Items)
                {
                    Array.Resize(ref serverMods, serverMods.Length + 1);
                    serverMods[serverMods.Length - 1] = "- " + item.ToString();
                }

                foreach (string item in clientDisplayMods.Items)
                {
                    Array.Resize(ref clientMods, clientMods.Length + 1);
                    clientMods[clientMods.Length - 1] = "- " + item.ToString();
                }

                string serverResult = string.Join("\n", serverMods);
                string clientResult = string.Join("\n", clientMods);

                launcherServerOutput.Clear();
                launcherServerOutput.Text += $"Server {Path.GetFileName(Properties.Settings.Default.server_path)} started! Port running: 6969\n";
                launcherServerOutput.Text += $"{serverDisplayMods.Items.Count + clientDisplayMods.Items.Count} mods running!\n\n";
                launcherServerOutput.Text += $"Active server mods:\n{serverResult}\n\n";
                launcherServerOutput.Text += $"Active client mods:\n{clientResult}\n\n";
                launcherServerOutput.Text += $"You can now run the AKI launcher!";

                lblLauncherServerInfo.Visible = true;
                lblLauncherServerInfo.Text =
                    $"Server {Path.GetFileName(Properties.Settings.Default.server_path)} running on port 6969\n" +
                    $"Running mods: {serverDisplayMods.Items.Count} server mods and {clientDisplayMods.Items.Count} client mods";

                btnLauncherEndProcess.Enabled = true;

                // StartLauncher();


            }
            else if (tickCount == 60)
            {
                MessageBox.Show("Server failed to run after 1 minute.", this.Text, MessageBoxButtons.OK);
            }
        }

        private bool IsPortOpen(int port)
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    tcpClient.Connect("localhost", port);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void StartLauncher()
        {
            string currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Properties.Settings.Default.server_path);
            Process launcher = new Process();

            launcher.StartInfo.WorkingDirectory = Properties.Settings.Default.server_path;
            launcher.StartInfo.FileName = "Aki.Launcher.exe";
            launcher.StartInfo.CreateNoWindow = true;
            launcher.StartInfo.UseShellExecute = false;
            launcher.StartInfo.RedirectStandardOutput = true;

            try
            {
                launcher.Start();
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }

            Directory.SetCurrentDirectory(currentDir);
        }

        private void serverDisplayMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            readServerMod();
        }

        private void clientDisplayMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            readClientMod();
        }

        private void watermark_Click(object sender, EventArgs e)
        {
            Process.Start("https://icons8.com");
        }

        private void serverAuthorToggle_Click(object sender, EventArgs e)
        {
            if (lblServerModAuthor.ReadOnly == true)
            {
                lblServerModAuthor.ReadOnly = false;
                lblServerModAuthor.ForeColor = Color.DodgerBlue;
            } else
            {
                if (darkTheme)
                {
                    lblServerModAuthor.ReadOnly = true;
                    lblServerModAuthor.ForeColor = Color.Silver;
                }
                else
                {
                    lblServerModAuthor.ReadOnly = true;
                    lblServerModAuthor.ForeColor = Color.Black;
                }
            }
        }

        private void serverVersionToggle_Click(object sender, EventArgs e)
        {
            if (lblServerModVersion.ReadOnly == true)
            {
                lblServerModVersion.ReadOnly = false;
                lblServerModVersion.ForeColor = Color.DodgerBlue;
            }
            else
            {
                if (darkTheme)
                {
                    lblServerModVersion.ReadOnly = true;
                    lblServerModVersion.ForeColor = Color.Silver;
                }
                else
                {
                    lblServerModVersion.ReadOnly = true;
                    lblServerModVersion.ForeColor = Color.Black;
                }
            }
        }

        private void serverSrcToggle_Click(object sender, EventArgs e)
        {
            if (lblServerModSrc.ReadOnly == true)
            {
                lblServerModSrc.ReadOnly = false;
                lblServerModSrc.ForeColor = Color.DodgerBlue;
            }
            else
            {
                if (darkTheme)
                {
                    lblServerModSrc.ReadOnly = true;
                    lblServerModSrc.ForeColor = Color.Silver;
                } else
                {
                    lblServerModSrc.ReadOnly = true;
                    lblServerModSrc.ForeColor = Color.Black;
                }
            }
        }

        private void serverAkiToggle_Click(object sender, EventArgs e)
        {
            if (lblServerModAkiVersion.ReadOnly == true)
            {
                lblServerModAkiVersion.ReadOnly = false;
                lblServerModAkiVersion.ForeColor = Color.DodgerBlue;
            }
            else
            {
                if (darkTheme)
                {
                    lblServerModAkiVersion.ReadOnly = true;
                    lblServerModAkiVersion.ForeColor = Color.Silver;
                } else
                {
                    lblServerModAkiVersion.ReadOnly = true;
                    lblServerModAkiVersion.ForeColor = Color.Black;
                }
            }
        }

        private void serverNameToggle_Click(object sender, EventArgs e)
        {
            if (lblServerModName.ReadOnly == true)
            {
                lblServerModName.ReadOnly = false;
                lblServerModName.ForeColor = Color.DodgerBlue;
            }
            else
            {
                if (darkTheme)
                {
                    lblServerModName.ReadOnly = true;
                    lblServerModName.ForeColor = Color.Silver;
                }
                else
                {
                    lblServerModName.ReadOnly = true;
                    lblServerModName.ForeColor = Color.Black;
                }
            }
        }

        private void clientTypeToggle_Click(object sender, EventArgs e)
        {
            
        }

        private void clientNameToggle_Click(object sender, EventArgs e)
        {
            if (lblClientModName.ReadOnly == true)
            {
                lblClientModName.ReadOnly = false;
                lblClientModName.ForeColor = Color.DodgerBlue;
            }
            else
            {
                lblClientModName.ReadOnly = true;
                lblClientModName.ForeColor = Color.Black;
            }
        }

        private void lblClientModName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                e.SuppressKeyPress = true;
                try
                {
                    if (lblClientModType.Text.ToLower().Contains("file"))
                    {
                        string[] clientmods = Directory.GetFiles(modsFolder);
                        for (int i = 0; i < clientmods.Length; i++)
                        {
                            if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                            {
                                btnClientModDisable.Enabled = true;

                                if (!lblClientModName.Text.Contains(".dll"))
                                {
                                    string selectedMod = Path.Combine(modsFolder, clientDisplayMods.SelectedItem.ToString());
                                    string DLLFile = Path.Combine(modsFolder, lblClientModName.Text);

                                    File.Move(selectedMod, $"{DLLFile}.dll");
                                    MessageBox.Show($"Updated mod name from {clientDisplayMods.SelectedItem.ToString()} to {lblClientModName.Text}", this.Text, MessageBoxButtons.OK);
                                }
                                else
                                {
                                    MessageBox.Show("Please do not include the extension (.dll) when renaming a mod.", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    } else
                    {
                        string[] clientmods = Directory.GetDirectories(modsFolder);
                        for (int i = 0; i < clientmods.Length; i++)
                        {
                            if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                            {
                                btnClientModDisable.Enabled = true;

                                if (MessageBox.Show($"Are you sure you want to rename {Path.GetFileName(clientmods[i])} to {lblClientModName.Text}?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    string selectedMod = Path.Combine(modsFolder, clientDisplayMods.SelectedItem.ToString());
                                    string modFolder = Path.Combine(modsFolder, lblClientModName.Text);

                                    CopyDirectory(selectedMod, modFolder, true);
                                    MessageBox.Show($"Updated mod name from {clientDisplayMods.SelectedItem.ToString()} to {lblClientModName.Text}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void lblServerModAuthor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] servermods = Directory.GetDirectories(modsFolder);
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                            string packagejsonFile = Path.Combine(selectedMod, "package.json");
                            string packagejson = File.ReadAllText(packagejsonFile);
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejsonFile, output);

                            MessageBox.Show($"Author has been changed to {lblServerModAuthor.Text}!", this.Text, MessageBoxButtons.OK);
                        } else if (i == servermods.Length)
                        {
                            MessageBox.Show($"We\'re sorry, it appears that {serverDisplayMods.SelectedItem.ToString()}\'s package.json can't be accessed. Maybe try refreshing?", this.Text, MessageBoxButtons.OK);
                        }
                    }


                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void lblServerModVersion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] servermods = Directory.GetDirectories(modsFolder);
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                            string packagejsonFile = Path.Combine(selectedMod, "package.json");
                            string packagejson = File.ReadAllText(packagejsonFile);
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejsonFile, output);

                            MessageBox.Show($"Version has been changed to {lblServerModVersion.Text}!", this.Text, MessageBoxButtons.OK);
                        }
                        else if (i == servermods.Length)
                        {
                            MessageBox.Show($"We\'re sorry, it appears that {serverDisplayMods.SelectedItem.ToString()}\'s package.json can't be accessed. Maybe try refreshing?", this.Text, MessageBoxButtons.OK);
                        }
                    }


                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void lblServerModSrc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] servermods = Directory.GetDirectories(modsFolder);
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                            string packagejsonFile = Path.Combine(selectedMod, "package.json");
                            string packagejson = File.ReadAllText(packagejsonFile);
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejsonFile, output);

                            MessageBox.Show($"Source has been changed to {lblServerModSrc.Text}!", this.Text, MessageBoxButtons.OK);
                        }
                        else if (i == servermods.Length)
                        {
                            MessageBox.Show($"We\'re sorry, it appears that {serverDisplayMods.SelectedItem.ToString()}\'s package.json can't be accessed. Maybe try refreshing?", this.Text, MessageBoxButtons.OK);
                        }
                    }


                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void lblServerModAkiVersion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                            string packagejsonFile = Path.Combine(selectedMod, "package.json");
                            string packagejson = File.ReadAllText(packagejsonFile);
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejsonFile, output);

                            MessageBox.Show($"Aki Version has been changed to {lblServerModAkiVersion.Text}!", this.Text, MessageBoxButtons.OK);
                        }
                        else if (i == servermods.Length)
                        {
                            MessageBox.Show($"We\'re sorry, it appears that {serverDisplayMods.SelectedItem.ToString()}\'s package.json can't be accessed. Maybe try refreshing?", this.Text, MessageBoxButtons.OK);
                        }
                    }


                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void lblServerModName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                            string packagejsonFile = Path.Combine(selectedMod, "package.json");
                            string packagejson = File.ReadAllText(packagejsonFile);
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejsonFile, output);

                            string selectedNew = Path.Combine(modsFolder, lblServerModName.Text);
                            CopyDirectory(selectedMod, selectedNew, true);
                            refreshUI();

                            MessageBox.Show($"Mod name has been changed to {lblServerModName.Text}!", this.Text, MessageBoxButtons.OK);
                        }
                        else if (i == servermods.Length)
                        {
                            MessageBox.Show($"We\'re sorry, it appears that {serverDisplayMods.SelectedItem.ToString()}\'s package.json can't be accessed. Maybe try refreshing?", this.Text, MessageBoxButtons.OK);
                        }
                    }


                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void displayServerPath_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", serverPath.Text);

            } catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void serverPath_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", serverPath.Text);
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void dragdropWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void dragdropWindow_DragDrop(object sender, DragEventArgs e)
        {
        }

        private void dragdropWindow_DragLeave(object sender, EventArgs e)
        {
        }

        private void bRefresh_Click(object sender, EventArgs e)
        {
        }

        private void mainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void mainWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);

            int counter = 0;
            string[] arr = { };

            foreach (string item in items)
            {
                counter++;
                FileAttributes attr = File.GetAttributes(item);

                if (item.EndsWith(".zip") || item.EndsWith(".7z"))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(item))
                    {
                        string path = Path.Combine(documentsFolder, Path.GetFileName(item));
                        archive.ExtractToDirectory(path);

                        string EFTExe = Path.Combine(item, "EscapeFromTarkov.exe");
                        string AkiExe = Path.Combine(item, "Aki.Server.exe");
                        string PackageFile = Path.Combine(path, "package.json");

                        string clientModsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                        string serverModsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");

                        string selectedClientMod = Path.Combine(clientModsFolder, Path.GetFileName(item));
                        string selectedServerMod = Path.Combine(serverModsFolder, Path.GetFileName(item));

                        if (path.EndsWith(".dll"))
                        {
                            // Mod is a client file
                            File.Move(path, selectedClientMod);
                            isClientMod = true;
                            clientModCount++;

                        }
                        else
                        {
                            if (File.Exists(EFTExe) && File.Exists(AkiExe))
                            {
                                // Server folder, so put into server path
                                lblAppServerPath.Text = item;
                                serverBool = true;

                            }
                            else if (!File.Exists(PackageFile))
                            {
                                // Client mod folder
                                try
                                {
                                    string[] identical = Directory.GetDirectories(path);
                                    foreach (string subfolder in identical)
                                    {
                                        string subPackageFile = Path.Combine(subfolder, "package.json");
                                        if (File.Exists(subPackageFile))
                                        {
                                            string subFolder = Path.Combine(serverModsFolder, Path.GetFileName(subfolder));
                                            CopyDirectory(subfolder, subFolder, true);

                                            Array.Resize(ref arr, arr.Length + 1);
                                            arr[arr.Length - 1] = $"-> {Path.GetFileName(path)} to user\\mods{Environment.NewLine}";

                                            int count = Int32.Parse(counterClientMods.Text);
                                            count++;
                                            counterServerMods.Text = count.ToString();

                                        } else
                                        {
                                            string subFolder = Path.Combine(clientModsFolder, Path.GetFileName(subfolder));
                                            CopyDirectory(subfolder, subFolder, true);

                                            Array.Resize(ref arr, arr.Length + 1);
                                            arr[arr.Length - 1] = $"-> {Path.GetFileName(path)} to BepInEx\\plugins{Environment.NewLine}";

                                            int count = Int32.Parse(counterClientMods.Text);
                                            count++;
                                            counterClientMods.Text = count.ToString();
                                            clientModCount++;

                                        }
                                    }

                                    Directory.Delete(path, true);
                                    isClientMod = true;
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine(err);
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                            else
                            {
                                // Server mod folder
                                try
                                {
                                    CopyDirectory(path, selectedServerMod, true);
                                    Array.Resize(ref arr, arr.Length + 1);
                                    arr[arr.Length - 1] = $"-> {Path.GetFileName(path)} to user\\mods{Environment.NewLine}";

                                    int count = Int32.Parse(counterServerMods.Text);
                                    count++;
                                    counterServerMods.Text = count.ToString();

                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine(err);
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }

                    }
                }
                else
                {
                    string EFTExe = Path.Combine(item, "EscapeFromTarkov.exe");
                    string AkiExe = Path.Combine(item, "Aki.Server.exe");
                    string PackageFile = Path.Combine(item, "package.json");

                    if ((attr & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory) // not a folder
                    {
                        // It's a file, so client mod
                        try
                        {
                            string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                            string selectedMod = Path.Combine(modsFolder, Path.GetFileName(item));

                            File.Move(item, selectedMod);
                            Array.Resize(ref arr, arr.Length + 1);
                            arr[arr.Length - 1] = $"-> {Path.GetFileName(item)} to BepInEx\\plugins{Environment.NewLine}";

                            isClientMod = true;
                            clientModCount++;
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err);
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }
                    }
                    else
                    {
                        if (File.Exists(EFTExe) && File.Exists(AkiExe))
                        {
                            // Server folder, so put into server path
                            lblAppServerPath.Text = item;
                            serverBool = true;
                        }
                        else if (!File.Exists(PackageFile))
                        {
                            // Client mod folder
                            try
                            {
                                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                                string selectedMod = Path.Combine(modsFolder, Path.GetFileName(item));

                                CopyDirectory(item, selectedMod, true);
                                Array.Resize(ref arr, arr.Length + 1);
                                arr[arr.Length - 1] = $"-> {Path.GetFileName(item)} to BepInEx\\plugins{Environment.NewLine}";

                                isClientMod = true;
                                clientModCount++;
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine(err);
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                        else
                        {
                            // Server mod folder
                            try
                            {
                                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                                string selectedMod = Path.Combine(modsFolder, Path.GetFileName(item));

                                CopyDirectory(item, selectedMod, true);
                                Array.Resize(ref arr, arr.Length + 1);
                                arr[arr.Length - 1] = $"-> {Path.GetFileName(item)} to user\\mods{Environment.NewLine}";
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine(err);
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }
                }
            }

            if (serverBool)
            {
                refreshUI();
                MessageBox.Show($"Set new server path!", this.Text);
                serverBool = false;
            } else
            {
                refreshUI();
                MessageBox.Show($"Successfully transferred {counter} mods.\n\n{string.Join("\n", arr)}", this.Text);

                if (isClientMod)
                {
                    if (clientModCount == 1)
                    {
                        MessageBox.Show($"A client mod was just installed.\n\nPlease launch the game once to ensure the app can access the config file(s).", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (clientModCount > 1)
                    {
                        MessageBox.Show($"{clientModCount} client mods was just installed.\n\nPlease launch the game once to ensure the app can access the config file(s).", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                        
                }
                    
            }
        }

        private void topPanel_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void serverPath_MouseEnter(object sender, EventArgs e)
        {
            serverPath.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Underline);
        }

        private void serverPath_MouseLeave(object sender, EventArgs e)
        {
            serverPath.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
        }

        private void btnClientModDisable_Click(object sender, EventArgs e)
        {
            if (clientConfig.TextLength > 0)
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                if (MessageBox.Show("A config is currently open, do you still want to disable this mod?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (lblClientModType.Text.ToLower().Contains("file"))
                    {
                        // Mod is a client file
                        string[] clientFiles = Directory.GetFiles(modsFolder);
                        foreach (string file in clientFiles)
                        {
                            if (Path.GetFileName(file) == clientDisplayMods.Text)
                            {
                                try
                                {
                                    string selectedMod = Path.Combine(documentsDisabledClientFolder, Path.GetFileName(file));
                                    File.Move(file, selectedMod);
                                    MessageBox.Show($"Disabled mod \"{Path.GetFileName(file)}\" -> Disabled Client Mods", this.Text);

                                    clientDisplayConfig.Items.Clear();
                                    refreshUI();
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Mod is a folder
                        string[] clientFolders = Directory.GetDirectories(modsFolder);
                        foreach (string folder in clientFolders)
                        {
                            if (Path.GetFileName(folder) == clientDisplayMods.Text)
                            {
                                try
                                {
                                    string selectedMod = Path.Combine(documentsDisabledClientFolder, Path.GetFileName(folder));
                                    CopyDirectory(folder, selectedMod, true);
                                    MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Client Mods", this.Text);

                                    clientDisplayConfig.Items.Clear();
                                    refreshUI();
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    }
                }
            } else
            {
                if (lblClientModType.Text.ToLower().Contains("file"))
                {
                    // Mod is a client file
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                    string[] clientFiles = Directory.GetFiles(modsFolder);
                    foreach (string file in clientFiles)
                    {
                        if (Path.GetFileName(file) == clientDisplayMods.Text)
                        {
                            try
                            {
                                string selectedMod = Path.Combine(documentsDisabledClientFolder, Path.GetFileName(file));
                                File.Move(file, selectedMod);
                                MessageBox.Show($"Disabled mod \"{Path.GetFileName(file)}\" -> Disabled Client Mods", this.Text);

                                clientDisplayConfig.Items.Clear();
                                refreshUI();
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }
                }
                else
                {
                    // Mod is a folder
                    string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                    string[] clientFolders = Directory.GetDirectories(modsFolder);
                    foreach (string folder in clientFolders)
                    {
                        if (Path.GetFileName(folder) == clientDisplayMods.Text)
                        {
                            try
                            {
                                string selectedMod = Path.Combine(documentsDisabledClientFolder, Path.GetFileName(folder));
                                CopyDirectory(folder, selectedMod, true);
                                MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Client Mods", this.Text);

                                clientDisplayConfig.Items.Clear();
                                refreshUI();
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }

                }
            }

        }

        private void btnServerModDisable_Click(object sender, EventArgs e)
        {
            if (serverConfig.TextLength > 0)
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                if (MessageBox.Show("A config is currently open, do you still want to disable this mod?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (newModLoader)
                    {
                        // 0.13
                        if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                        {
                            // Mod is a folder
                            string[] servermods = Directory.GetDirectories(modsFolder);
                            foreach (string folder in servermods)
                            {
                                if (Path.GetFileName(folder) == serverDisplayMods.Text)
                                {
                                    try
                                    {
                                        string selectedMod = Path.Combine(documentsDisabledServerFolder, Path.GetFileName(folder));
                                        CopyDirectory(folder, selectedMod, true);
                                        MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                        string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                                        string orderJSON = File.ReadAllText(orderFile);
                                        JObject order = JObject.Parse(orderJSON);
                                        JArray array = ((JArray)order["order"]);
                                        array.Clear();

                                        serverDisplayMods.Items.Remove(serverDisplayMods.SelectedItem);
                                        foreach (var item in serverDisplayMods.Items)
                                        {
                                            array.Add(item.ToString());
                                        }

                                        string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                                        File.WriteAllText(orderFile, output);

                                        serverDisplayMods.Items.Clear();
                                        btnServerModDelete.Enabled = false;

                                        resetUI();
                                        refreshUI();
                                    }
                                    catch (Exception err)
                                    {
                                        Debug.WriteLine($"ERROR: {err}");
                                        MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // 0.12 or pre
                        if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                        {
                            // Mod is a folder
                            string[] servermods = Directory.GetDirectories(modsFolder);
                            foreach (string folder in servermods)
                            {
                                if (Path.GetFileName(folder) == serverDisplayMods.Text)
                                {
                                    try
                                    {
                                        string selectedMod = Path.Combine(documentsDisabledServerFolder, Path.GetFileName(folder));
                                        CopyDirectory(folder, selectedMod, true);
                                        MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                        serverDisplayMods.Items.Remove(serverDisplayMods.SelectedItem);

                                        serverDisplayMods.Items.Clear();
                                        btnServerModDelete.Enabled = false;

                                        resetUI();
                                        refreshUI();
                                    }
                                    catch (Exception err)
                                    {
                                        Debug.WriteLine($"ERROR: {err}");
                                        MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                    }
                                }
                            }
                        }

                    }

                }
            } else
            {
                if (newModLoader)
                {
                    // 0.13
                    if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                    {
                        // Mod is a folder
                        string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                        string[] servermods = Directory.GetDirectories(modsFolder);
                        foreach (string folder in servermods)
                        {
                            if (Path.GetFileName(folder) == serverDisplayMods.Text)
                            {
                                try
                                {
                                    string selectedMod = Path.Combine(documentsDisabledServerFolder, Path.GetFileName(folder));
                                    CopyDirectory(folder, selectedMod, true);
                                    MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                    string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                                    string orderJSON = File.ReadAllText(orderFile);
                                    JObject order = JObject.Parse(orderJSON);
                                    JArray array = ((JArray)order["order"]);
                                    array.Clear();

                                    serverDisplayMods.Items.Remove(serverDisplayMods.SelectedItem);
                                    foreach (var item in serverDisplayMods.Items)
                                    {
                                        array.Add(item.ToString());
                                    }

                                    string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                                    File.WriteAllText(orderFile, output);

                                    serverDisplayMods.Items.Clear();
                                    btnServerModDelete.Enabled = false;

                                    resetUI();
                                    refreshUI();
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 0.12 or pre
                    if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                    {
                        // Mod is a folder
                        string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                        string[] servermods = Directory.GetDirectories(modsFolder);
                        foreach (string folder in servermods)
                        {
                            if (Path.GetFileName(folder) == serverDisplayMods.Text)
                            {
                                try
                                {
                                    string selectedMod = Path.Combine(documentsDisabledServerFolder, Path.GetFileName(folder));
                                    CopyDirectory(folder, selectedMod, true);
                                    MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                    serverDisplayMods.Items.Remove(serverDisplayMods.SelectedItem);

                                    serverDisplayMods.Items.Clear();
                                    btnServerModDelete.Enabled = false;

                                    resetUI();
                                    refreshUI();
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                            }
                        }
                    }

                }
            }
        }

        private void lblDisabledModToggle_Click(object sender, EventArgs e)
        {
            if (lblDisabledModType.Text.ToLower().Contains("server"))
            {
                // Mod is a server mod
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                string[] servermods = Directory.GetDirectories(documentsDisabledServerFolder);
                foreach (string folder in servermods)
                {
                    if (Path.GetFileName(folder) == disabledDisplayMods.Text)
                    {
                        try
                        {
                            string selectedMod = Path.Combine(modsFolder, Path.GetFileName(folder));
                            CopyDirectory(folder, selectedMod, true);
                            MessageBox.Show($"Enabled mod \"{Path.GetFileName(folder)}\" -> user\\mods", this.Text);

                            serverDisplayMods.Items.Clear();
                            refreshServerUI();
                            refreshDisabledUI();
                            countMods();

                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine($"ERROR: {err}");
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }
                    }
                }

            } else if (lblDisabledModType.Text.ToLower().Contains("client"))
            {
                string disabledMod = Path.Combine(documentsDisabledClientFolder, disabledDisplayMods.Text);
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                if (!File.Exists(disabledMod))
                {
                    // Mod is a folder
                    string[] clientfolders = Directory.GetDirectories(documentsDisabledClientFolder);
                    foreach (string folder in clientfolders)
                    {
                        if (Path.GetFileName(folder) == disabledDisplayMods.Text)
                        {
                            try
                            {
                                string selectedMod = Path.Combine(modsFolder, Path.GetFileName(folder));
                                CopyDirectory(folder, selectedMod, true);
                                MessageBox.Show($"Enabled mod \"{Path.GetFileName(folder)}\" -> BepInEx\\plugins", this.Text);

                                clientDisplayMods.Items.Clear();
                                refreshClientUI();
                                refreshDisabledUI();
                                countMods();
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }

                } else
                {
                    string[] clientfiles = Directory.GetFiles(documentsDisabledClientFolder);
                    foreach (string file in clientfiles)
                    {
                        if (Path.GetFileName(file) == disabledDisplayMods.Text)
                        {
                            try
                            {
                                string selectedMod = Path.Combine(modsFolder, Path.GetFileName(file));
                                File.Move(file, selectedMod);
                                MessageBox.Show($"Enabled client mod \"{Path.GetFileName(file)}\" -> BepInEx\\plugins", this.Text);

                                clientDisplayMods.Items.Clear();
                                refreshClientUI();
                                refreshDisabledUI();
                                countMods();
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                        }
                    }

                }
            }
        }

        private void disabledDisplayMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            readDisabledMod();
        }

        private void panelSettings_Click(object sender, EventArgs e)
        {
        }

        private void btnAppServerBrowse_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = serverPath.Text;
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                lblAppServerPath.Text = dialog.FileName;
                serverPath.Text = dialog.FileName;
                Properties.Settings.Default.server_path = dialog.FileName;
                Properties.Settings.Default.Save();
                Application.Restart();
            }
        }

        private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void btnAppReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure want to reset the app?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Properties.Settings.Default.Reset();
                Application.Restart();
            }
        }

        private void serverModsPanel_Enter(object sender, EventArgs e)
        {

        }

        private void counterServerMods_Click(object sender, EventArgs e)
        {
            string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
            Process.Start("explorer.exe", modsFolder);
        }

        private void counterClientMods_Click(object sender, EventArgs e)
        {
            string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
            Process.Start("explorer.exe", modsFolder);
        }

        private void counterDisabledMods_Click(object sender, EventArgs e)
        {

            Process.Start("explorer.exe", documentsFolder);
        }

        private void serverDisplayConfigs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selectedMod = serverDisplayMods.SelectedItem.ToString();
                string selected = serverDisplayConfigs.SelectedItem.ToString();
                string modFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");

                string fullPath = fullPaths[serverDisplayConfigs.SelectedIndex];
                serverConfigTitle.Text = fullPath;

                string read = File.ReadAllText(fullPath);
                dynamic json = JsonConvert.DeserializeObject<dynamic>(read);
                serverConfig.Text = json.ToString();

                /*
                if (Directory.Exists($"{modFolder}\\{selectedMod}\\config"))
                {
                    if (File.Exists($"{modFolder}\\" + // user\mods
                    $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                    $"{serverDisplayConfigs.Tag.ToString()}\\" +
                    $"{serverDisplayConfigs.SelectedItem.ToString()}"))
                    {
                        string read = File.ReadAllText($"{modFolder}\\" + // user\mods
                        $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                        $"{serverDisplayConfigs.Tag.ToString()}\\" +
                        $"{serverDisplayConfigs.SelectedItem.ToString()}"); // config folder -> config file

                        dynamic json = JsonConvert.DeserializeObject<dynamic>(read);

                        serverConfig.Text = json.ToString();
                    }

                    serverConfigTitle.Text = $"{modFolder}\\" + // user\mods
                        $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                        $"{serverDisplayConfigs.Tag.ToString()}\\" +
                        $"{serverDisplayConfigs.SelectedItem.ToString()}"; // config folder -> config file

                }
                else if (Directory.Exists($"{modFolder}\\{selectedMod}\\cfg"))
                {
                    if (File.Exists($"{modFolder}\\" + // user\mods
                    $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                    $"{serverDisplayConfigs.Tag.ToString()}\\" +
                    $"{serverDisplayConfigs.SelectedItem.ToString()}"))
                    {
                        string read = File.ReadAllText($"{modFolder}\\" + // user\mods
                        $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                        $"{serverDisplayConfigs.Tag.ToString()}\\" +
                        $"{serverDisplayConfigs.SelectedItem.ToString()}"); // config folder -> config file

                        dynamic json = JsonConvert.DeserializeObject<dynamic>(read);

                        serverConfig.Text = json.ToString();
                    }

                    serverConfigTitle.Text = $"{modFolder}\\" + // user\mods
                        $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                        $"{serverDisplayConfigs.Tag.ToString()}\\" +
                        $"{serverDisplayConfigs.SelectedItem.ToString()}"; // config folder -> config file

                }
                else
                {
                    if (File.Exists($"{modFolder}\\{selectedMod}\\{selected}"))
                    {
                        string read = File.ReadAllText($"{modFolder}\\" +
                            $"{serverDisplayMods.SelectedItem.ToString()}\\" +
                            $"{serverDisplayConfigs.SelectedItem.ToString()}"); // config file
                        dynamic json = JsonConvert.DeserializeObject<dynamic>(read);

                        serverConfig.Text = json.ToString();
                    }

                    serverConfigTitle.Text = $"{modFolder}\\" + // user\mods
                        $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                        $"{serverDisplayConfigs.SelectedItem.ToString()}"; // config file
                }
                */

                Properties.Settings.Default.configReserve = serverConfig.Text;
                btnServerConfigOpen.Enabled = true;
                btnServerConfigValidate.Enabled = true;
                btnServerConfigRestore.Enabled = true;

                lblServerConfigPlaceholder.Visible = true;
                serverConfigTitle.Visible = true;
                btnServerConfigToggle.Enabled = true;

                serverConfig.ReadOnly = false;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnServerConfigValidate_Click(object sender, EventArgs e)
        {
            try
            {
                string read = serverConfig.Text;
                var obj = JsonConvert.DeserializeObject(read);

                if (MessageBox.Show($"Validation successful, config saved!", this.Text, MessageBoxButtons.OK) == DialogResult.OK)
                {
                    try
                    {
                        string configFile = serverConfigTitle.Text;
                        JObject json = JObject.Parse(read);
                        string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                        File.WriteAllText(configFile, output);

                        displayConfigSuccess(false);
                        topPanel.Select();
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine($"ERROR: {err}");
                        MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                    }

                    /*
                    btnServerConfigSave.Enabled = true;
                    serverConfig.ReadOnly = true;
                    btnServerConfigValidate.Enabled = false;
                    */
                }

                serverConfig.SelectionBackColor = Color.FromArgb(255, 45, 45, 45);
                serverConfig.SelectionColor = Color.LightGray;

                serverConfig.BackColor = Color.FromArgb(45, 45, 45);
                serverConfig.ForeColor = Color.LightGray;
                string refresh = serverConfig.Text;

                serverConfig.Text = "";
                serverConfig.Text = refresh;

            }
            catch (JsonReaderException ex)
            {
                Color originalBackColor;
                Color originalForeColor;

                if (ex.LineNumber > 0)
                {
                    int line = ex.LineNumber - 1;
                    int start = serverConfig.GetFirstCharIndexFromLine(line - 1);
                    int end = serverConfig.GetFirstCharIndexFromLine(line);

                    serverConfig.Select(start, end - start);
                    originalBackColor = serverConfig.SelectionBackColor;
                    originalForeColor = serverConfig.SelectionColor;
                    serverConfig.SelectionBackColor = Color.FromArgb(170, 255, 0, 0);
                    serverConfig.SelectionColor = Color.White;


                    System.Timers.Timer timer = new System.Timers.Timer();
                    timer.Interval = 10000; // 7,5 seconds
                    serverConfig.ScrollToCaret();

                    timer.Elapsed += (tsender, b) =>
                    {
                        serverConfig.Invoke((MethodInvoker)delegate
                        {
                            serverConfig.SelectionBackColor = originalBackColor;
                            serverConfig.SelectionColor = originalForeColor;
                            timer.Stop();
                        });
                    };
                    timer.Start();

                    if (MessageBox.Show($"Validation failed at line {ex.LineNumber - 1}. Would you like to see the full error?.", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        MessageBox.Show(ex.Message.ToString(), this.Text, MessageBoxButtons.OK);
                    }

                }
                
            }

        }

        private void serverConfig_TextChanged(object sender, EventArgs e)
        {

        }

        private void serverConfig_KeyDown(object sender, KeyEventArgs e)
        {
            if (serverConfig.ReadOnly == true)
                e.SuppressKeyPress = true;
        }

        private void btnServerConfigSave_Click(object sender, EventArgs e)
        {
            //
        }

        private void clientDisplayConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selected = clientDisplayConfig.SelectedItem.ToString();
                string configsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\config");

                string[] configs = Directory.GetFiles(configsFolder);
                foreach (string config in configs)
                {
                    if (Path.GetFileName(config).ToLower().Contains(selected.ToString().ToLower()))
                    {
                        string activecfg = Path.Combine(configsFolder, Path.GetFileName(config));
                        string read = File.ReadAllText(activecfg);

                        clientConfig.Text = read;
                        clientConfigTitle.Text = activecfg;

                        clientConfigPlaceholder.Visible = true;
                        clientConfigTitle.Visible = true;
                        clientConfig.ReadOnly = false;

                        btnClientConfigOpen.Enabled = true;
                        btnClientConfigSave.Enabled = true;

                    }
                }

            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnServerSortAlphabetical_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Sort mods alphabetically?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                    string orderJSON = File.ReadAllText(orderFile);
                    JObject order = JObject.Parse(orderJSON);
                    JArray array = ((JArray)order["order"]);
                    array.Clear();

                    var list = new List<string>();
                    foreach (string item in serverDisplayMods.Items)
                    {
                        list.Add(item.ToString());
                    }

                    var sorted = list.OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase).ToArray();

                    foreach (var item in sorted)
                    {
                        array.Add(item.ToString());
                    }

                    string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                    File.WriteAllText(orderFile, output);
                    displayAlphabeticalSort();

                    serverDisplayMods.Items.Clear();
                    loadServerMods();
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnServerSortUp_Click(object sender, EventArgs e)
        {
            if (serverDisplayMods.SelectedIndex > -1)
            {
                try
                {
                    // An item is selected
                    int originalIndex = serverDisplayMods.SelectedIndex;
                    string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                    string orderJSON = File.ReadAllText(orderFile);
                    JObject order = JObject.Parse(orderJSON);
                    JArray array = ((JArray)order["order"]);

                    if (serverDisplayMods.SelectedItem != null)
                    {
                        MoveBoxItemUp(serverDisplayMods);
                        array.Clear();

                        foreach (var item in serverDisplayMods.Items)
                        {
                            array.Add(item.ToString());
                        }

                        string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                        File.WriteAllText(orderFile, output);

                        int newIndex = serverDisplayMods.SelectedIndex;
                        displayMoveSuccess(true, serverDisplayMods.SelectedItem.ToString(), originalIndex, newIndex);
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }
        }

        private void btnServerSortDown_Click(object sender, EventArgs e)
        {
            if (serverDisplayMods.SelectedIndex > -1)
            {
                try
                {
                    // An item is selected
                    int originalIndex = serverDisplayMods.SelectedIndex;
                    string orderFile = Path.Combine(Properties.Settings.Default.server_path, "user\\mods\\order.json");
                    string orderJSON = File.ReadAllText(orderFile);
                    JObject order = JObject.Parse(orderJSON);
                    JArray array = ((JArray)order["order"]);

                    if (serverDisplayMods.SelectedItem != null)
                    {
                        MoveBoxItemDown(serverDisplayMods);
                        array.Clear();

                        foreach (var item in serverDisplayMods.Items)
                        {
                            array.Add(item.ToString());
                        }

                        string output = JsonConvert.SerializeObject(order, Formatting.Indented);
                        File.WriteAllText(orderFile, output);

                        int newIndex = serverDisplayMods.SelectedIndex;
                        displayMoveSuccess(false, serverDisplayMods.SelectedItem.ToString(), originalIndex, newIndex);
                    }
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }

                
            }
        }

        private void btnClientConfigSave_Click(object sender, EventArgs e)
        {
            try
            {
                string read = clientConfig.Text;
                string configFile = clientConfigTitle.Text;
                string orderJSON = File.ReadAllText(configFile);
                File.WriteAllText(configFile, orderJSON);

                displayConfigSuccess(true);
                topPanel.Select();
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnClientConfigOpen_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(clientConfigTitle.Text);
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnClientModDelete_Click(object sender, EventArgs e)
        {
            if (clientConfig.TextLength > 0)
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                if (MessageBox.Show("A config is currently open, do you still want to delete this mod?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string selectedMod = Path.Combine(modsFolder, lblClientModName.Text);
                    string item = selectedMod;
                    if (MessageBox.Show($"Do you wish to delete {clientDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        if (lblClientModType.Text.ToLower().Contains("file"))
                        {
                            if (!File.Exists(item))
                            {
                                MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }
                            else
                            {
                                try
                                {
                                    File.Delete(item);
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                                MessageBox.Show($"{lblClientModName.Text} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);


                            }

                        }
                        else
                        {
                            if (!Directory.Exists(item))
                            {
                                MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }
                            else
                            {
                                try
                                {
                                    Directory.Delete(item, true);
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                                MessageBox.Show($"{lblClientModName.Text} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            }

                        }


                        resetUI();
                        refreshClientUI();
                    }
                }
            } else
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "BepInEx\\plugins");
                string selectedMod = Path.Combine(modsFolder, lblClientModName.Text);
                string item = selectedMod;
                if (MessageBox.Show($"Do you wish to delete {clientDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (lblClientModType.Text.ToLower().Contains("file"))
                    {
                        if (!File.Exists(item))
                        {
                            MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                        else
                        {
                            try
                            {
                                File.Delete(item);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                            MessageBox.Show($"{lblClientModName.Text} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }

                    }
                    else
                    {
                        if (!Directory.Exists(item))
                        {
                            MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                        else
                        {
                            try
                            {
                                Directory.Delete(item, true);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                            MessageBox.Show($"{lblClientModName.Text} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }

                    }

                    resetUI();
                    refreshClientUI();
                }
            }
        }

        private void btnServerConfigOpen_Click(object sender, EventArgs e)
        {
            Process.Start(serverConfigTitle.Text);
        }

        private void btnDisabledModDelete_Click(object sender, EventArgs e)
        {
            bool isDone = false;
            if (MessageBox.Show($"Do you wish to delete {lblDisabledModName.Text}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (lblDisabledModType.Text.ToLower().Contains("client"))
                {
                    string[] files = Directory.GetFiles(documentsDisabledClientFolder);
                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).ToLower() == lblDisabledModName.Text.ToLower())
                        try
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine($"ERROR: {err}");
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }
                    }

                    string[] clientfolders = Directory.GetDirectories(documentsDisabledClientFolder);
                    if (!isDone)
                    {
                        foreach (string clientfolder in clientfolders)
                        {
                            if (Path.GetFileName(clientfolder).ToLower() == lblDisabledModName.Text.ToLower())
                            {
                                try
                                {
                                    Directory.Delete(clientfolder, true);
                                }
                                catch (Exception err)
                                {
                                    Debug.WriteLine($"ERROR: {err}");
                                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                                }
                                isDone = true;
                                break;
                            }
                        }
                    }

                    refreshDisabledUI();
                }
                else if (lblDisabledModType.Text.ToLower().Contains("server"))
                {
                    string[] serverfolders = Directory.GetDirectories(documentsDisabledServerFolder);

                    foreach (string serverfolder in serverfolders)
                    {
                        if (Path.GetFileName(serverfolder).ToLower() == lblDisabledModName.Text.ToLower())
                        {
                            try
                            {
                                Directory.Delete(serverfolder, true);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                            isDone = true;
                            break;
                        }
                    }

                    refreshDisabledUI();
                }
            }
        }

        private void displayProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            readProfile();
        }

        private void bRestartApp_Click(object sender, EventArgs e)
        {
        }

        private void btnServerModDelete_Click(object sender, EventArgs e)
        {
            if (serverConfig.TextLength > 0)
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                if (MessageBox.Show("A config is currently open, do you still want to delete this mod?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                    string item = selectedMod;
                    if (MessageBox.Show($"Do you wish to delete {serverDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        if (!Directory.Exists(item))
                        {
                            MessageBox.Show($"{serverDisplayMods.SelectedItem.ToString()} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            try
                            {
                                Directory.Delete(item, true);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine($"ERROR: {err}");
                                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                            }
                            MessageBox.Show($"{serverDisplayMods.SelectedItem.ToString()} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }

                        refreshServerUI();
                    }
                }
            }
            else
            {
                string modsFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\mods");
                string selectedMod = Path.Combine(modsFolder, serverDisplayMods.SelectedItem.ToString());
                string item = selectedMod;
                if (MessageBox.Show($"Do you wish to delete {serverDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (!Directory.Exists(item))
                    {
                        MessageBox.Show($"{serverDisplayMods.SelectedItem.ToString()} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        try
                        {
                            Directory.Delete(item, true);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine($"ERROR: {err}");
                            MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                        }
                        MessageBox.Show($"{serverDisplayMods.SelectedItem.ToString()} deleted succesfully.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }

                    refreshServerUI();
                }
            }
        }

        private void managerMenu_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        void onDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string cleanText = Regex.Replace(e.Data, "\033\\[[0-9;]*m", "");
                launcherServerOutput.Invoke(new Action(() =>
                {
                    launcherServerOutput.AppendText($"{cleanText}\r\n");
                }));
            }
        }

        private void btnLauncherEndProcess_Click(object sender, EventArgs e)
        {

            try
            {
                if (processToKill != null)
                {
                    processToKill.Kill();

                    if (processToKill.HasExited)
                    {
                        try
                        {
                            processToKill.Kill();
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show("Ended server process!", this.Text, MessageBoxButtons.OK);
                            launcherServerOutput.Clear();
                            btnLauncherEndProcess.Enabled = false;
                            btnLauncherStartProcess.Enabled = true;

                            lblLauncherServerInfo.Visible = false;
                        }
                    } else
                    {
                        MessageBox.Show("Failed to end server process, we apologize for the inconvenience. Please manually force-close the server.\n", this.Text, MessageBoxButtons.OK);
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }


            try
            {
                if (launcherProcess != null)
                {
                    launcherProcess.Kill();

                    if (launcherProcess.HasExited)
                    {
                        try
                        {
                            launcherProcess.Kill();
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show("Ended server process!", this.Text, MessageBoxButtons.OK);
                            launcherServerOutput.Clear();
                            btnLauncherEndProcess.Enabled = false;
                            btnLauncherStartProcess.Enabled = true;

                            lblLauncherServerInfo.Visible = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to end server process, we apologize for the inconvenience. Please manually force-close the server.\n", this.Text, MessageBoxButtons.OK);
                    }
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void launcherServerOutput_MouseDown(object sender, MouseEventArgs e)
        {
            topPanel.Select();
        }

        private void serverConfig_MouseDown(object sender, MouseEventArgs e)
        {
            /*
            if (serverDisplayConfigs.SelectedIndex > -1 && serverDisplayConfigs.Text != "" || serverDisplayConfigs.Text != null)
            {
                if (serverConfig.TextLength > 0)
                {
                    int index = serverConfig.GetCharIndexFromPosition(e.Location);
                    int line = serverConfig.GetLineFromCharIndex(index);
                    int start = serverConfig.GetFirstCharIndexFromLine(line);
                    int length = serverConfig.Lines[line].Length;
                    serverConfig.SelectionStart = start;
                    serverConfig.SelectionLength = length;
                } else
                {
                    topPanel.Select();
                }
            }
            */
        }

        private void btnServerConfigToggle_Click(object sender, EventArgs e)
        {
            if (serverDisplayConfigs.SelectedIndex > -1 && serverDisplayConfigs.Text != "" || serverDisplayConfigs.Text != null)
            {
                if (serverConfig.TextLength > 0)
                {
                    int currentLine = serverConfig.GetLineFromCharIndex(serverConfig.SelectionStart);
                    int startIndex = serverConfig.GetFirstCharIndexFromLine(currentLine);
                    int length = serverConfig.Lines[currentLine].Length;
                    string line = serverConfig.Lines[currentLine];
                    if (line.Contains("true"))
                    {
                        serverConfig.Select(startIndex, length);
                        serverConfig.SelectedText = line.Replace("true", "false");
                    }
                    else if (line.Contains("false"))
                    {
                        serverConfig.Select(startIndex, length);
                        serverConfig.SelectedText = line.Replace("false", "true");
                    }
                } else
                {
                    topPanel.Select();
                }

            }
        }

        private void serverConfig_Leave(object sender, EventArgs e)
        {
            serverConfig.SelectionLength = 0;
        }

        private void serverConfig_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (serverDisplayConfigs.SelectedIndex > -1 && serverDisplayConfigs.Text != "" || serverDisplayConfigs.Text != null)
            {
                if (serverConfig.TextLength > 0)
                {
                    int currentLine = serverConfig.GetLineFromCharIndex(serverConfig.SelectionStart);
                    int startIndex = serverConfig.GetFirstCharIndexFromLine(currentLine);
                    int length = serverConfig.Lines[currentLine].Length;
                    string line = serverConfig.Lines[currentLine];
                    if (line.Contains("true"))
                    {
                        serverConfig.Select(startIndex, length);
                        serverConfig.SelectedText = line.Replace("true", "false");
                    }
                    else if (line.Contains("false"))
                    {
                        serverConfig.Select(startIndex, length);
                        serverConfig.SelectedText = line.Replace("false", "true");
                    }
                }
                
            }
                
        }

        private void btnServerConfigRestore_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Restore to when the config first loaded?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                serverConfig.Text = Properties.Settings.Default.configReserve;
                MessageBox.Show("Restored!", this.Text, MessageBoxButtons.OK);
            }
        }

        private void bTheme_Click(object sender, EventArgs e)
        {
            if (darkTheme)
            {
                ToggleTheme(false);
                darkTheme = false;
            } else
            {
                ToggleTheme(true);
                darkTheme = true;
            }
        }

        private void btnProfileDelete_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This feature is incomplete, we apologize for the inconvenience!", this.Text, MessageBoxButtons.OK);
        }

        private void btnLauncherStartProcess_Click(object sender, EventArgs e)
        {
            string cacheFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\cache");
            if (Directory.Exists(cacheFolder))
            {
                try
                {
                    Directory.Delete(cacheFolder, true);
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                    MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                }
            }

            // Server
            string currentDirServer = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(curDir);

            processToKill = new Process();
            processToKill.StartInfo.WorkingDirectory = Properties.Settings.Default.server_path;
            processToKill.StartInfo.FileName = "Aki.Server.exe";
            processToKill.StartInfo.CreateNoWindow = false;
            processToKill.StartInfo.UseShellExecute = false;
            processToKill.StartInfo.RedirectStandardOutput = false;
            processToKill.OutputDataReceived += new DataReceivedEventHandler(onDataReceived);

            try
            {
                processToKill.Start();
                this.WindowState = FormWindowState.Normal;
                this.Focus();

                // StartTimer();
                // btnLauncherStartProcess.Enabled = false;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }

            Directory.SetCurrentDirectory(currentDirServer);


            // Client
            string currentDirClient = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(curDir);

            launcherProcess = new Process();
            launcherProcess.StartInfo.WorkingDirectory = Properties.Settings.Default.server_path;
            launcherProcess.StartInfo.FileName = "Aki.Launcher.exe";
            launcherProcess.StartInfo.CreateNoWindow = false;
            launcherProcess.StartInfo.UseShellExecute = false;
            launcherProcess.StartInfo.RedirectStandardOutput = false;
            launcherProcess.OutputDataReceived += new DataReceivedEventHandler(onDataReceived);

            try
            {
                launcherProcess.Start();
                this.WindowState = FormWindowState.Normal;
                this.Focus();

                // StartTimer();
                // btnLauncherStartProcess.Enabled = false;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }

            Directory.SetCurrentDirectory(currentDirClient);
        }

        private void btnLauncherStartLauncher_Click(object sender, EventArgs e)
        {
            // StartLauncher();
        }

        private void btnAppClearCache_Click(object sender, EventArgs e)
        {
            try
            {
                string cacheFolder = Path.Combine(Properties.Settings.Default.server_path, "user\\cache");
                if (Directory.Exists(cacheFolder))
                {
                    Directory.Delete(cacheFolder, true);
                    MessageBox.Show($"Cleared cache for server {Path.GetFileName(Properties.Settings.Default.server_path)}!", this.Text, MessageBoxButtons.OK);
                }
                else
                {
                    MessageBox.Show($"No cache folder detected for server {Path.GetFileName(Properties.Settings.Default.server_path)}, you\'re good to go!", this.Text, MessageBoxButtons.OK);
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void lblAppProfileEditingToggle_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.profileEditing == true)
            {
                Properties.Settings.Default.profileEditing = false;
                lblAppProfileEditingToggle.Text = "Disabled";
                lblAppProfileEditingToggle.ForeColor = Color.IndianRed;

                displayProfiles.Enabled = false;
                lblProfileCharacterId.Enabled = false;
                lblProfileUsername.Enabled = false;
                lblProfileGameUsername.Enabled = false;
                lblProfilePassword.Enabled = false;
                lblProfileEdition.Enabled = false;

                btnProfileDelete.Enabled = false;
            } else
            {
                Properties.Settings.Default.profileEditing = true;
                lblAppProfileEditingToggle.Text = "Enabled";
                lblAppProfileEditingToggle.ForeColor = Color.DodgerBlue;

                displayProfiles.Enabled = true;
                lblProfileCharacterId.Enabled = true;
                lblProfileUsername.Enabled = true;
                lblProfileGameUsername.Enabled = true;
                lblProfilePassword.Enabled = true;

                lblProfileEdition.Enabled = true;
            }

            Properties.Settings.Default.Save();
        }

        private void lblAppLauncherToggle_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.dedicatedLauncher == true)
            {
                Properties.Settings.Default.dedicatedLauncher = false;
                lblAppLauncherToggle.Text = "Disabled";
                lblAppLauncherToggle.ForeColor = Color.IndianRed;

                btnLauncherStartProcess.Enabled = false;
                // btnLauncherStartLauncher.Enabled = false;
            }
            else
            {
                Properties.Settings.Default.dedicatedLauncher = true;
                lblAppLauncherToggle.Text = "Enabled";
                lblAppLauncherToggle.ForeColor = Color.DodgerBlue;

                btnLauncherStartProcess.Enabled = true;
                // btnLauncherStartLauncher.Enabled = true;
            }

            Properties.Settings.Default.Save();
        }

        private void lblProfileCharacterId_TextChanged(object sender, EventArgs e)
        {
        }

        private void btnAppRefreshUI_Click(object sender, EventArgs e)
        {
            try
            {
                resetUI();
                refreshUI();
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void appServerSettings_Enter(object sender, EventArgs e)
        {

        }

        private void btnAppRestart_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show($"Restart {this.Text}?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    Application.Restart();
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnAppViewChangelog_Click(object sender, EventArgs e)
        {
            if (appChangelogBox.Visible)
            {
                appChangelogBox.Visible = false;
                btnAppViewChangelog.Text = "Open changelog";

            } else
            {
                appChangelogBox.Visible = true;
                btnAppViewChangelog.Text = "Close changelog";

            }
        }

        private void appChangelog_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }
}
