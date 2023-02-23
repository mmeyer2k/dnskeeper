using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace dnskeeper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Add items to the adapter combobox
            foreach (NetworkInterface Adapter in Helpers.GetAdapters())
            {
                comboBox1.Items.Add(Adapter.Name);
            }

            // Prep the interface combo box
            comboBox1.SelectedIndex = 0;
            comboBox1.Focus();

            // Add IP textbox validation events
            textBox1.TextChanged += textBoxIP_TextChanged;
            textBox2.TextChanged += textBoxIP_TextChanged;
            textBox3.TextChanged += textBoxIP_TextChanged;
            textBox4.TextChanged += textBoxIP_TextChanged;

            // Add dynamic menu items
            DynamicMenuReload();

            // Make sure that the profiles menu item is disabled when form loads
            menuProfiles.Enabled = false;

            // Autoload ethernet for now...
            string autoload = Helpers.Settings().GetValue("Autoload")?.ToString();
            if (autoload != null && comboBox1.Items.IndexOf(autoload) != -1)
            {
                comboBox1.SelectedIndex = comboBox1.Items.IndexOf(autoload);
            }
        }

        #region Form events
        private void button1_Click(object sender, EventArgs e)
        {
            string adapter = comboBox1.Text;

            Helpers.SetDns(adapter, 4, checkBox1, textBox1, textBox2);
            Helpers.SetDns(adapter, 6, checkBox2, textBox3, textBox4);

            MessageBox.Show("DNS settings saved (hopefully)");

            // Refresh the form by simulating a flow cycle
            int selectedAdapter = comboBox1.SelectedIndex;
            comboBox1.SelectedIndex = 0;
            comboBox1.SelectedIndex = selectedAdapter;
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox1.Text = "";
                textBox2.Text = "";
            }
            else
            {
                textBox1.Enabled = true;
                textBox2.Enabled = true;
            }
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                textBox3.Enabled = false;
                textBox4.Enabled = false;
                textBox3.Text = "";
                textBox4.Text = "";
            }
            else
            {
                textBox3.Enabled = true;
                textBox4.Enabled = true;
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lock user interface
            LoadingAnimationShow();

            if (comboBox1.SelectedIndex == 0)
            {
                return;
            }

            string[] servers = Helpers.GetAdapterDnsAddresses(comboBox1.Text);

            // Load form with the adapters currently configured DNS settings
            LoadProfileIntoForm(servers);

            // Load the "current settings" groupBoxes with the current adapter DNS settings
            string[] ipv4 = Array.FindAll(servers, s => s.Contains("."));
            string[] ipv6 = Array.FindAll(servers, s => s.Contains(":"));
            label1.Text = ipv4.Length != 0 ? ipv4[0] : "";
            label2.Text = ipv4.Length == 2 ? ipv4[1] : "";
            label3.Text = ipv6.Length != 0 ? ipv6[0] : "";
            label4.Text = ipv6.Length == 2 ? ipv6[1] : "";

            // Un-lock the user interface
            LoadingAnimationHide();

            comboBox1.Focus();
        }
        private void textBoxIP_TextChanged(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;

            string ip = textbox.Text;

            bool valid = textbox.Text.Length == 0 || Helpers.IsIPv4Address(ip) || Helpers.IsIPv6Address(ip);

            textbox.ForeColor = valid ? Color.Black : Color.Red;

            button1.Enabled =
                textBox1.ForeColor != Color.Red &&
                textBox2.ForeColor != Color.Red &&
                textBox3.ForeColor != Color.Red &&
                textBox4.ForeColor != Color.Red;
        }
        #endregion

        #region Menu item click events
        private void AutoloadAdapter_Click(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;

            item.Checked = true;

            if (item.Text == "None")
            {
                Helpers.Settings().DeleteValue("Autoload");
            }
            else
            {
                Helpers.Settings().SetValue("Autoload", item.Text);
            }

            DynamicMenuReload();
        }
        private void DeleteProfileMenuItemClick(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;

            Helpers.Profiles().DeleteValue(item.Text);

            DynamicMenuReload();
        }
        private void menuAbout_Click(object sender, EventArgs e)
        {
            Form2 newWindow = new Form2();

            newWindow.ShowDialog();
        }
        private void menuExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private void menuReload_Click(object sender, EventArgs e)
        {
            var selected = comboBox1.SelectedIndex;
            comboBox1.SelectedIndex = 0;
            comboBox1.SelectedIndex = selected;
        }
        private void FactoryResetMenuItem_Click(object sender, EventArgs e)
        {
            string msg = "This will reset all profiles and settings! Are you sure?";

            DialogResult prompt = MessageBox.Show(msg, "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

            if (prompt == DialogResult.Yes)
            {
                Array.ForEach(Helpers.Settings().GetValueNames(), s => Helpers.Settings().DeleteValue(s));
                Array.ForEach(Helpers.Profiles().GetValueNames(), s => Helpers.Profiles().DeleteValue(s));
            }

            DynamicMenuReload();
        }
        private void NewProfileMenuItem_Click(object sender, EventArgs e)
        {
            string prompt = $"IPv4:  {textBox1.Text}\nIPv4:  {textBox2.Text}\n\nIPv6:  {textBox3.Text}\nIPv6:  {textBox4.Text}\n\nCreate a name:";

            string name = Interaction.InputBox(prompt, "Save new profile?", "New profile").Trim().ToString();

            string error = "";

            if (name == "")
            {
                error = "Nothing was saved, but nothing was lost either ;-)";
            }

            if (Helpers.Profiles().GetValue(name)?.ToString().Length > 0)
            {
                error = "An item with this name already exists in your saved profiles.";
            }

            if (error.Length > 0)
            {
                MessageBox.Show(error, "Oh goodness...", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            string[] json = { textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text };

            Helpers.Profiles().SetValue(name, new JavaScriptSerializer().Serialize(json));

            DynamicMenuReload();
        }
        private void ProfileMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;

            if (Helpers.Profiles().GetValue(item.Text)?.ToString().Length > 0)
            {
                // Unserialise profile data
                string setting = Helpers.Profiles().GetValue(item.Text).ToString();
                string[] json = new JavaScriptSerializer().Deserialize<string[]>(setting);

                LoadProfileIntoForm(json);

                return;
            }

            // Try to find/load the profile from the default profiles list
            LoadProfileIntoForm(Helpers.defaultProfiles[item.Text]);
        }
        #endregion

        #region UI-related helper functions
        private void LoadingAnimationHide()
        {
            menuProfiles.Enabled = true;
            button1.Show();
            groupBox1.Show();
            groupBox2.Show();
            groupBox3.Show();
            groupBox4.Show();
        }
        private void LoadingAnimationShow()
        {
            menuProfiles.Enabled = false;
            button1.Hide();
            groupBox1.Hide();
            groupBox2.Hide();
            groupBox3.Hide();
            groupBox4.Hide();
        }
        private void LoadProfileIntoForm(string[] addresses)
        {
            string[] ipv4 = Array.FindAll(addresses, s => s.Contains("."));
            string[] ipv6 = Array.FindAll(addresses, s => s.Contains(":"));

            textBox1.Text = ipv4.Length != 0 ? ipv4[0] : "";
            textBox2.Text = ipv4.Length == 2 ? ipv4[1] : "";

            textBox3.Text = ipv6.Length != 0 ? ipv6[0] : "";
            textBox4.Text = ipv6.Length == 2 ? ipv6[1] : "";

            checkBox1.Checked = textBox1.Text.Length == 0 && textBox2.Text.Length == 0;
            checkBox2.Checked = textBox3.Text.Length == 0 && textBox4.Text.Length == 0;
        }
        private void DynamicMenuReload()
        {
            menuProfiles.MenuItems.Clear();

            // Add "new profile" button to profiles menu
            MenuItem newProfile = new MenuItem("&New profile");
            newProfile.Click += new EventHandler(NewProfileMenuItem_Click);
            menuProfiles.MenuItems.Add(newProfile);

            // Add user-defined profiles to menu
            string[] values = Helpers.Profiles().GetValueNames();
            if (values.Length > 0)
            {
                MenuItem deleteProfile = new MenuItem("&Delete profile");

                foreach (string profile in values)
                {
                    MenuItem deleteProfileItem = new MenuItem(profile);
                    deleteProfileItem.Click += new EventHandler(DeleteProfileMenuItemClick);
                    deleteProfile.MenuItems.Add(deleteProfileItem);
                }

                menuProfiles.MenuItems.Add(deleteProfile);

                menuProfiles.MenuItems.Add("-");

                foreach (string profile in values)
                {
                    MenuItem item = new MenuItem(profile);
                    item.Click += new EventHandler(ProfileMenuItem_Click);
                    menuProfiles.MenuItems.Add(item);
                }
            }

            menuProfiles.MenuItems.Add("-");

            // Add default profiles to the menu
            foreach (KeyValuePair<string, string[]> profile in Helpers.defaultProfiles)
            {
                menuProfiles.MenuItems.Add(new MenuItem(profile.Key, ProfileMenuItem_Click));
            }

            // Clear adapter auto-load list
            menuAutoLoadAdapter.MenuItems.Clear();

            // Get the currently saved autoload adapter
            string selected = Helpers.Settings().GetValue("Autoload")?.ToString();

            // Add the "none" option to the adapter auto-load list
            menuAutoLoadAdapter.MenuItems.Add(new MenuItem("None", AutoloadAdapter_Click) {
                Checked = selected == null,
                Enabled = selected != null ,
                RadioCheck = true,
            });
            menuAutoLoadAdapter.MenuItems.Add("-");

            // Add interfaces to the adapter auto-load list
            foreach (NetworkInterface Adapter in Helpers.GetAdapters())
            { 
                menuAutoLoadAdapter.MenuItems.Add(new MenuItem(Adapter.Name, AutoloadAdapter_Click)
                {
                    Checked = selected == Adapter.Name,
                    Enabled = selected != Adapter.Name,
                    RadioCheck = true,
                });
            }
        }
        #endregion
    }
}
