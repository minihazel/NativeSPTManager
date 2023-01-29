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

namespace NativeSPTManager
{
    public partial class mainWindow : Form
    {
        public string curDir;
        public string configjson;
        public string documentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SPT Manager");
        public string documentsDisabledServerFolder;
        public string documentsDisabledClientFolder;
        public bool serverBool = false;
        public bool isClientMod = false;
        public bool newModLoader = false;
        public int clientModCount = 0;

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
                if (MessageBox.Show($"It looks like {this.Text} has no server to connect to. Would you like to fix this?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            }
        }

        private void checkGameVersion()
        {
            string orderFile = $"{Properties.Settings.Default.server_path}\\Aki_Data\\Server\\configs\\core.json";
            string orderJSON = File.ReadAllText(orderFile);
            JObject order = JObject.Parse(orderJSON);

            this.Text = $"{this.Text} - {order["akiVersion"].ToString()} ({order["compatibleTarkovVersion"].ToString()})";

            if (order["compatibleTarkovVersion"].ToString().Contains("0.13"))
                checkOrderJSON();

        }

        private void checkOrderJSON()
        {
            string order = $"{Properties.Settings.Default.server_path}\\user\\mods\\order.json";
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
                string orderFile = $"{curDir}\\user\\mods\\order.json";
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
            if (this.Text.Contains("0.12") && newModLoader == false)
            {
                try
                {
                    string[] serverFolders = Directory.GetDirectories($"{curDir}\\user\\mods");
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
                    string orderFile = $"{curDir}\\user\\mods\\order.json";
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
            try
            {
                string[] clientFolders = Directory.GetDirectories($"{curDir}\\BepInEx\\plugins");
                foreach (string folder in clientFolders)
                {
                    clientDisplayMods.Items.Add(Path.GetFileName(folder));
                }

                string[] clientFiles = Directory.GetFiles($"{curDir}\\BepInEx\\plugins");
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
            try
            {
                string[] clientfolders = Directory.GetDirectories($"{documentsDisabledClientFolder}");
                string[] clientfiles = Directory.GetFiles($"{documentsDisabledClientFolder}");
                string[] servermods = Directory.GetDirectories($"{documentsDisabledServerFolder}");

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
                string[] profiles = Directory.GetFiles($"{curDir}\\user\\profiles");
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

            btnServerConfigSave.Enabled = false;
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

            btnServerConfigSave.Enabled = false;
            btnServerConfigValidate.Enabled = false;
            btnServerConfigOpen.Enabled = false;
            btnServerConfigToggle.Enabled = false;

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
                serverDisplayConfigs.Items.Clear();

                string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                for (int i = 0; i < servermods.Length; i++)
                {
                    if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                    {
                        enableServerButtons();

                        string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                        JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                        lblServerModAuthor.Text = json["author"].ToString();
                        lblServerModVersion.Text = json["version"].ToString();
                        lblServerModSrc.Text = json["main"].ToString();
                        lblServerModAkiVersion.Text = json["akiVersion"].ToString();
                        lblServerModName.Text = json["name"].ToString();

                        serverDisplayConfigs.Items.Clear();

                        string[] subfolders = Directory.GetDirectories(servermods[i]);
                        foreach (string folder in subfolders)
                        {
                            if (Path.GetFileName(folder) == "config" || Path.GetFileName(folder) == "cfg")
                            {
                                string[] configfiles = Directory.GetFiles(folder);
                                foreach (string cfg in configfiles)
                                {
                                    serverDisplayConfigs.Items.Add(Path.GetFileName(cfg));
                                    serverDisplayConfigs.Tag = $"config";

                                    serverConfigTitle.Text = "";
                                    serverConfig.Text = "";
                                    btnServerConfigToggle.Enabled = false;

                                    sortingTooltip.SetToolTip(btnServerSortUp, $"Move the load order up one slot on {serverDisplayMods.SelectedItem.ToString()}");
                                    sortingTooltip.SetToolTip(btnServerSortDown, $"Move the load order down one slot on {serverDisplayMods.SelectedItem.ToString()}");
                                }

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

        private void readClientMod()
        {
            if (clientDisplayMods.SelectedItem.ToString().EndsWith(".dll"))
            {
                // Mod is a file
                try
                {
                    clientDisplayConfig.Items.Clear();
                    string[] clientmods = Directory.GetFiles($"{curDir}\\BepInEx\\plugins");
                    for (int i = 0; i < clientmods.Length; i++)
                    {
                        if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                        {
                            enableClientButtons();

                            btnClientModDisable.Enabled = true;
                            btnServerModDelete.Enabled = true;
                            lblClientModType.Text = "File based";
                            lblClientModName.Text = Path.GetFileNameWithoutExtension(clientmods[i]);

                            try
                            {
                                // Checking for config file
                                string selected = Path.GetFileNameWithoutExtension(clientmods[i]).ToString().ToLower();
                                string configsFolder = $"{curDir}\\BepInEx\\config";

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
                                        btnClientModDelete.Enabled = true;

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
                    string[] clientmods = Directory.GetDirectories($"{curDir}\\BepInEx\\plugins");
                    for (int i = 0; i < clientmods.Length; i++)
                    {
                        if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                        {
                            btnClientModDisable.Enabled = true;
                            lblClientModType.Text = "Folder based";
                            lblClientModName.Text = Path.GetFileName(clientmods[i]);

                            try
                            {
                                // Checking for config file
                                string selected = Path.GetFileNameWithoutExtension(clientmods[i]).ToString().ToLower();
                                string configsFolder = $"{curDir}\\BepInEx\\config";

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
                    if (File.Exists($"{documentsDisabledServerFolder}\\{disabledDisplayMods.Text}\\package.json"))
                    {
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
                string packagejson = File.ReadAllText($"{curDir}\\user\\profiles\\{displayProfiles.SelectedItem.ToString()}");
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

                healthHead.Text = $"{headCur} / {headMax} hp";

                healthLeftArm.Text = $"{leftArmCur} / {leftArmMax} hp";
                healthRightArm.Text = $"{RightArmCur} / {RightArmMax} hp";

                healthChest.Text = $"{ChestCur} / {ChestMax} hp";
                healthStomach.Text = $"{StomachCur} / {StomachMax} hp";

                healthLeftLeg.Text = $"{LeftLegCur} / {LeftLegMax} hp";
                healthRightLeg.Text = $"{RightLegCur} / {RightLegMax} hp";

                healthTotal.Text = $"{totalHealth.ToString()} hp";

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
                string profileToRead = File.ReadAllText($"{curDir}\\user\\profiles\\{profileId}");

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

        private void displayMoveSuccess(bool direction, string modName)
        {
            if (!direction)
            {
                serverDisplayLabel.Visible = true;
                serverDisplayLabel.Text = $"Moved {modName} down one slot in order!";

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
                serverDisplayLabel.Text = $"Moved {modName} up one slot in order!";

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
                lblServerModAuthor.ReadOnly = true;
                lblServerModAuthor.ForeColor = Color.Black;
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
                lblServerModVersion.ReadOnly = true;
                lblServerModVersion.ForeColor = Color.Black;
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
                lblServerModSrc.ReadOnly = true;
                lblServerModSrc.ForeColor = Color.Black;
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
                lblServerModAkiVersion.ReadOnly = true;
                lblServerModAkiVersion.ForeColor = Color.Black;
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
                lblServerModName.ReadOnly = true;
                lblServerModName.ForeColor = Color.Black;
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
                e.SuppressKeyPress = true;
                try
                {
                    if (lblClientModType.Text.ToLower().Contains("file"))
                    {
                        string[] clientmods = Directory.GetFiles($"{curDir}\\BepInEx\\plugins");
                        for (int i = 0; i < clientmods.Length; i++)
                        {
                            if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                            {
                                btnClientModDisable.Enabled = true;

                                if (!lblClientModName.Text.Contains(".dll"))
                                {
                                    File.Move($"{curDir}\\BepInEx\\plugins\\{clientDisplayMods.SelectedItem.ToString()}",
                                             $"{curDir}\\BepInEx\\plugins\\{lblClientModName.Text}.dll");

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
                        string[] clientmods = Directory.GetDirectories($"{curDir}\\BepInEx\\plugins");
                        for (int i = 0; i < clientmods.Length; i++)
                        {
                            if (Path.GetFileName(clientmods[i]) == clientDisplayMods.SelectedItem.ToString())
                            {
                                btnClientModDisable.Enabled = true;

                                if (MessageBox.Show($"Are you sure you want to rename {Path.GetFileName(clientmods[i])} to {lblClientModName.Text}?", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {

                                    CopyDirectory($"{curDir}\\BepInEx\\plugins\\{clientDisplayMods.SelectedItem.ToString()}",
                                        $"{curDir}\\BepInEx\\plugins\\{lblClientModName.Text}", true);

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
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejson, output);
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
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejson, output);
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
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejson, output);
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
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejson, output);
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
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    for (int i = 0; i < servermods.Length; i++)
                    {
                        if (Path.GetFileName(servermods[i]) == serverDisplayMods.SelectedItem.ToString())
                        {
                            btnServerModDisable.Enabled = true;
                            string packagejson = File.ReadAllText($"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}\\package.json");
                            JObject json = JsonConvert.DeserializeObject<JObject>(packagejson);

                            json["author"] = lblServerModAuthor.Text;
                            json["version"] = lblServerModVersion.Text;
                            json["main"] = lblServerModSrc.Text;
                            json["akiVersion"] = lblServerModAkiVersion.Text;
                            json["name"] = lblServerModName.Text;

                            string output = JsonConvert.SerializeObject(json, Formatting.Indented);
                            File.WriteAllText(packagejson, output);

                            CopyDirectory($"{curDir}\\user\\mods\\{clientDisplayMods.SelectedItem.ToString()}",
                                $"{curDir}\\user\\mods\\{lblServerModName.Text}", true);
                            refreshUI();
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
                bRefresh.Visible = false;
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
            resetUI();
            refreshUI();
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

                if (item.EndsWith(".zip"))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(item))
                    {
                        string path = $"{documentsFolder}\\{Path.GetFileName(item)}";
                        archive.ExtractToDirectory(path);


                        if (path.EndsWith(".dll"))
                        {
                            // Mod is a client file
                            File.Move(path, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(item)}");
                            isClientMod = true;
                            clientModCount++;

                        }
                        else
                        {
                            if (File.Exists($"{path}\\EscapeFromTarkov.exe") && File.Exists($"{path}\\Aki.Server.exe"))
                            {
                                // Server folder, so put into server path
                                lblAppServerPath.Text = item;
                                serverBool = true;

                            }
                            else if (!File.Exists($"{path}\\package.json"))
                            {
                                // Client mod folder
                                try
                                {
                                    string[] identical = Directory.GetDirectories(path);
                                    foreach (string subfolder in identical)
                                    {
                                        if (File.Exists($"{subfolder}\\package.json"))
                                        {
                                            CopyDirectory(subfolder, $"{curDir}\\user\\mods\\{Path.GetFileName(subfolder)}", true);

                                            Array.Resize(ref arr, arr.Length + 1);
                                            arr[arr.Length - 1] = $"-> {Path.GetFileName(path)} to user\\mods{Environment.NewLine}";

                                            int count = Int32.Parse(counterClientMods.Text);
                                            count++;
                                            counterServerMods.Text = count.ToString();

                                        } else
                                        {
                                            CopyDirectory(subfolder, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(subfolder)}", true);

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
                                    CopyDirectory(path, $"{curDir}\\user\\mods\\{Path.GetFileName(item)}", true);
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
                } else
                {
                    if ((attr & FileAttributes.Directory) != FileAttributes.Directory) // not a folder
                    {
                        // It's a file, so client mod
                        try
                        {
                            File.Move(item, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(item)}");
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
                        if (File.Exists($"{item}\\EscapeFromTarkov.exe") && File.Exists($"{item}\\Aki.Server.exe"))
                        {
                            // Server folder, so put into server path
                            lblAppServerPath.Text = item;
                            serverBool = true;
                        }
                        else if (!File.Exists($"{item}\\package.json"))
                        {
                            // Client mod folder
                            try
                            {
                                CopyDirectory(item, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(item)}", true);
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
                                CopyDirectory(item, $"{curDir}\\user\\mods\\{Path.GetFileName(item)}", true);
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
            if (lblClientModType.Text.ToLower().Contains("file"))
            {
                // Mod is a client file
                string[] clientFiles = Directory.GetFiles($"{curDir}\\BepInEx\\plugins");
                foreach (string file in clientFiles)
                {
                    if (Path.GetFileName(file) == clientDisplayMods.Text)
                    {
                        try
                        {
                            File.Move(file, $"{documentsDisabledClientFolder}\\{Path.GetFileName(file)}");
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

            } else
            {
                // Mod is a folder
                string[] clientFolders = Directory.GetDirectories($"{curDir}\\BepInEx\\plugins");
                foreach (string folder in clientFolders)
                {
                    if (Path.GetFileName(folder) == clientDisplayMods.Text)
                    {
                        try
                        {
                            CopyDirectory(folder, $"{documentsDisabledClientFolder}\\{Path.GetFileName(folder)}", true);
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

        private void btnServerModDisable_Click(object sender, EventArgs e)
        {
            if (newModLoader)
            {
                // 0.13
                if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                {
                    // Mod is a folder
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    foreach (string folder in servermods)
                    {
                        if (Path.GetFileName(folder) == serverDisplayMods.Text)
                        {
                            try
                            {
                                CopyDirectory(folder, $"{documentsDisabledServerFolder}\\{Path.GetFileName(folder)}", true);
                                MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                string orderFile = $"{curDir}\\user\\mods\\order.json";
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
            } else
            {
                // 0.12 or pre
                if (serverDisplayMods.Text != "" || serverDisplayMods.Text != null)
                {
                    // Mod is a folder
                    string[] servermods = Directory.GetDirectories($"{curDir}\\user\\mods");
                    foreach (string folder in servermods)
                    {
                        if (Path.GetFileName(folder) == serverDisplayMods.Text)
                        {
                            try
                            {
                                CopyDirectory(folder, $"{documentsDisabledServerFolder}\\{Path.GetFileName(folder)}", true);
                                MessageBox.Show($"Disabled mod \"{Path.GetFileName(folder)}\" -> Disabled Server Mods", this.Text);

                                serverDisplayMods.Items.Remove(serverDisplayMods.SelectedItem);

                                serverDisplayMods.Items.Clear();
                                btnServerModDelete.Enabled = false;
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

        private void lblDisabledModToggle_Click(object sender, EventArgs e)
        {
            if (lblDisabledModType.Text.ToLower().Contains("server"))
            {
                // Mod is a server mod
                string[] servermods = Directory.GetDirectories(documentsDisabledServerFolder);
                foreach (string folder in servermods)
                {
                    if (Path.GetFileName(folder) == disabledDisplayMods.Text)
                    {
                        try
                        {
                            CopyDirectory(folder, $"{curDir}\\user\\mods\\{Path.GetFileName(folder)}", true);
                            MessageBox.Show($"Enabled mod \"{Path.GetFileName(folder)}\" -> user\\mods", this.Text);

                            serverDisplayMods.Items.Clear();
                            refreshServerUI();
                            refreshDisabledUI();

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
                if (!File.Exists($"{documentsDisabledClientFolder}\\{disabledDisplayMods.Text}"))
                {
                    // Mod is a folder
                    string[] clientfolders = Directory.GetDirectories(documentsDisabledClientFolder);
                    foreach (string folder in clientfolders)
                    {
                        if (Path.GetFileName(folder) == disabledDisplayMods.Text)
                        {
                            try
                            {
                                CopyDirectory(folder, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(folder)}", true);
                                MessageBox.Show($"Enabled mod \"{Path.GetFileName(folder)}\" -> BepInEx\\plugins", this.Text);

                                clientDisplayMods.Items.Clear();
                                refreshClientUI();
                                refreshDisabledUI();
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
                                File.Move(file, $"{curDir}\\BepInEx\\plugins\\{Path.GetFileName(file)}");
                                MessageBox.Show($"Enabled client mod \"{Path.GetFileName(file)}\" -> BepInEx\\plugins", this.Text);

                                clientDisplayMods.Items.Clear();
                                refreshClientUI();
                                refreshDisabledUI();
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
            Process.Start("explorer.exe", $"{curDir}\\user\\mods");
        }

        private void counterClientMods_Click(object sender, EventArgs e)
        {

            Process.Start("explorer.exe", $"{curDir}\\BepInEx\\plugins");
        }

        private void counterDisabledMods_Click(object sender, EventArgs e)
        {

            Process.Start("explorer.exe", documentsFolder);
        }

        private void serverDisplayConfigs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selected = serverDisplayConfigs.SelectedItem.ToString();
                string modFolder = $"{curDir}\\user\\mods";
                string read = File.ReadAllText($"{modFolder}\\" + // user\mods
                    $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                    $"{serverDisplayConfigs.Tag.ToString()}\\" +
                    $"{serverDisplayConfigs.SelectedItem.ToString()}"); // config folder -> config file

                dynamic json = JsonConvert.DeserializeObject<dynamic>(read);

                serverConfig.Text = json.ToString();
                serverConfigTitle.Text = $"{modFolder}\\" + // user\mods
                    $"{serverDisplayMods.SelectedItem.ToString()}\\" + // ModName
                    $"{serverDisplayConfigs.Tag.ToString()}\\" +
                    $"{serverDisplayConfigs.SelectedItem.ToString()}"; // config folder -> config file

                btnServerConfigOpen.Enabled = true;
                btnServerConfigValidate.Enabled = true;

                lblServerConfigPlaceholder.Visible = true;
                serverConfigTitle.Visible = true;
                btnServerConfigToggle.Enabled = true;

                serverConfig.ReadOnly = false;

            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err}");
                MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
            }
        }

        private void btnServerConfigValidate_Click(object sender, EventArgs e)
        {
            try
            {
                string read = serverConfig.Text;
                var obj = JsonConvert.DeserializeObject(read);

                if (MessageBox.Show($"Validation successful! You can now save via \"Save Config\".", this.Text, MessageBoxButtons.OK) == DialogResult.OK)
                {
                    btnServerConfigSave.Enabled = true;
                    serverConfig.ReadOnly = true;
                    btnServerConfigValidate.Enabled = false;
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

                    btnServerConfigSave.Enabled = false;

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
            try
            {
                string read = serverConfig.Text;
                string configFile = serverConfigTitle.Text;
                string orderJSON = File.ReadAllText(configFile);
                JObject json = JObject.Parse(orderJSON);
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
        }

        private void clientDisplayConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selected = clientDisplayConfig.SelectedItem.ToString();
                string configsFolder = $"{curDir}\\BepInEx\\config";

                string[] configs = Directory.GetFiles(configsFolder);
                foreach (string config in configs)
                {
                    if (Path.GetFileName(config).ToLower().Contains(selected.ToString().ToLower()))
                    {
                        string activecfg = $"{configsFolder}\\{Path.GetFileName(config)}";
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
                    string orderFile = $"{curDir}\\user\\mods\\order.json";
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
                    string orderFile = $"{curDir}\\user\\mods\\order.json";
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
                        displayMoveSuccess(true, serverDisplayMods.SelectedItem.ToString());
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
                    string orderFile = $"{curDir}\\user\\mods\\order.json";
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
                        displayMoveSuccess(false, serverDisplayMods.SelectedItem.ToString());
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
            string item = $"{curDir}\\BepInEx\\plugins\\{lblClientModName.Text}";
            if (MessageBox.Show($"Do you wish to delete {clientDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (lblClientModType.Text.ToLower().Contains("file"))
                {
                    if (!File.Exists(item))
                    {
                        MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        refreshClientUI();

                    } else
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

                        refreshClientUI();

                    }

                } else
                {
                    if (!Directory.Exists(item))
                    {
                        MessageBox.Show($"{lblClientModName.Text} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        refreshClientUI();

                    } else
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

                        refreshClientUI();

                    }

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
                if (lblDisabledModType.Text.ToLower().Contains("file"))
                {
                    string[] files = Directory.GetFiles($"{documentsDisabledClientFolder}\\{lblDisabledModName.Text}");
                    foreach (string file in files)
                    {
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

                    refreshDisabledUI();
                }
                else if (lblDisabledModType.Text.ToLower().Contains("folder"))
                {
                    string[] serverfolders = Directory.GetDirectories(documentsDisabledClientFolder);
                    string[] clientfolders = Directory.GetDirectories(documentsDisabledServerFolder);

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
                            refreshDisabledUI();
                            break;
                        }
                    }

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
                                refreshDisabledUI();
                                break;
                            }
                        }
                    }

                }
            }
        }

        private void displayProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            readProfile();
        }

        private void bRestartApp_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"Restart {this.Text}?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                Application.Restart();
            }
        }

        private void btnServerModDelete_Click(object sender, EventArgs e)
        {
            string item = $"{curDir}\\user\\mods\\{serverDisplayMods.SelectedItem.ToString()}";
            if (MessageBox.Show($"Do you wish to delete {serverDisplayMods.SelectedItem.ToString()}? This action is irreversible.", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (!Directory.Exists(item))
                {
                    MessageBox.Show($"{serverDisplayMods.SelectedItem.ToString()} does not appear to exist, removing its info.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    refreshServerUI();

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

                    refreshServerUI();

                }
            }
        }

        private void managerMenu_SelectedIndexChanged(object sender, EventArgs e)
        {
            tabControl1.SelectTab(5);

            if (managerMenu.SelectedIndex > -1)
            {
                if (managerMenu.SelectedItem.ToString().ToLower() == "launch spt-aki")
                {
                    string currentDir = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(curDir);
                    Process spt = new Process();
                    spt.StartInfo.FileName = "Aki.Server.exe";
                    spt.StartInfo.CreateNoWindow = true;
                    spt.StartInfo.UseShellExecute = false;
                    spt.StartInfo.RedirectStandardOutput = true;
                    spt.OutputDataReceived += new DataReceivedEventHandler(onDataReceived);

                    try
                    {
                        spt.Start();
                        spt.BeginOutputReadLine();

                        Directory.SetCurrentDirectory(currentDir);
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine($"ERROR: {err.Message.ToString()}");
                        MessageBox.Show($"Oops! It seems like we received an error. If you're uncertain what it\'s about, please message the developer with a screenshot:\n\n{err.Message.ToString()}", this.Text, MessageBoxButtons.OK);
                    }


                }

            }

            managerMenu.SelectedIndex = -1;
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
                Process[] processes = Process.GetProcessesByName("Node.js JavaScript Runtime");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        try
                        {
                            string processPath = process.MainModule.FileName;
                            if (Path.GetFileName(processPath).Equals("Aki.Server.exe"))
                            {
                                process.Kill();
                                MessageBox.Show("Yay");
                                break;
                            }
                        }
                        catch (Exception err)
                        {
                            // Handle exception
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

        private void launcherServerOutput_MouseDown(object sender, MouseEventArgs e)
        {
            topPanel.Select();
        }

        private void serverConfig_MouseDown(object sender, MouseEventArgs e)
        {
            int index = serverConfig.GetCharIndexFromPosition(e.Location);
            int line = serverConfig.GetLineFromCharIndex(index);
            int start = serverConfig.GetFirstCharIndexFromLine(line);
            int length = serverConfig.Lines[line].Length;
            serverConfig.SelectionStart = start;
            serverConfig.SelectionLength = length;
        }

        private void btnServerConfigToggle_Click(object sender, EventArgs e)
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

        private void serverConfig_Leave(object sender, EventArgs e)
        {
            serverConfig.SelectionLength = 0;
        }

        private void serverConfig_MouseDoubleClick(object sender, MouseEventArgs e)
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
