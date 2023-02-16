using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace dnskeeper
{
    static class Helpers
    {
        /// <summary>
        /// A listing of default DNS profiles in format { ipv4#1, ipv4#2, ipv6#1, ipv6#2 }
        /// </summary>
        public static Dictionary<string, string[]> defaultProfiles = new Dictionary<string, string[]>()
        {
            { "DHCP discovery", new string[4] { "", "", "", "" } },
            { "OpenDNS", new string[4] { "208.67.222.222", "208.67.220.220", "2620:119:35::35", "2620:119:53::53" } },
            { "CloudFlare", new string[4] { "1.1.1.1", "1.0.0.1", "2606:4700:4700::1111", "2606:4700:4700::1001" } },
            { "Google", new string[4] { "8.8.8.8", "8.8.4.4", "2001:4860:4860::8888", "2001:4860:4860::8844" } },
            { "Blackhole", new string[4] { "0.0.0.0", "", "::", "" } },
            { "Localhost", new string[4] { "127.0.0.1", "", "::1", "" } },
        };

        /// <summary>
        /// Get dictionary of network adapters keyed by name
        /// </summary>
        /// <returns>Returns dictionary of adapters</returns>
        public static string[] GetAdapterDnsAddresses(string adapter)
        {
            IPAddressCollection x = Array.Find(Helpers.GetAdapters(), c => c.Name == adapter).GetIPProperties().DnsAddresses;
            
            List<string> list = new List<string>();
            
            foreach (IPAddress y in x)
            {
                list.Add(y.ToString());
            }

            return list.ToArray();
        }

        public static NetworkInterface[] GetAdapters()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            return Array.FindAll(interfaces, n => !n.Name.Contains("Loopback") && !n.Name.Contains("WSL"));
        }

        /// <summary>
        /// Determines if IPv4 address is valid
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>Returns true if IP is valid</returns>
        public static bool IsIPv4Address(string ip)
        {
            if (String.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            string[] splitValues = ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        /// <summary>
        /// Determines if IPv6 address is valid
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsIPv6Address(string ip)
        {
            IPAddress address;

            if (IPAddress.TryParse(ip, out address))
            {
                return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
            }

            return false;
        }

        /// <summary>
        /// Runs netsh via the shell.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="debug"></param>
        /// <returns>The raw string output of the netsh command</returns>
        public static string Netsh(string command, bool debug = false)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo() { 
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "cmd.exe",
                Arguments = $"/C netsh {command}",
            };

            using (Process exeProcess = Process.Start(startInfo))
            {
                string stdout = exeProcess.StandardOutput.ReadToEnd();
                string stderr = exeProcess.StandardError.ReadToEnd();

                string detail = $"# netsh {command}\nstdout: {stdout}\nstderr:{stderr}\n";

                exeProcess.WaitForExit();

                return debug ? detail : stdout;
            }
        }

        /// <summary>
        /// Set system DNS from form elements
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="ipv"></param>
        /// <param name="dhcp"></param>
        /// <param name="dns1"></param>
        /// <param name="dns2"></param>
        public static void SetDns(string adapter, int ipv, CheckBox dhcp, TextBox dns1, TextBox dns2)
        {
            Helpers.Netsh($"interface ipv{ipv} delete dns name=\"{adapter}\" all");

            if (dhcp.Checked || (dns1.Text.Length == 0 && dns2.Text.Length == 0))
            {
                Helpers.Netsh($"interface ipv{ipv} set dns name=\"{adapter}\" dhcp");

                return;
            }

            if (dns1.Text.Length > 0)
            {
                Helpers.Netsh($"interface ipv{ipv} set dns name=\"{adapter}\" static {dns1.Text} validate=no");
            }

            if (dns2.Text.Length > 0)
            {
                Helpers.Netsh($"interface ipv{ipv} add dns name=\"{adapter}\" {dns2.Text} index=2 validate=no");
            }
        }

        /// <summary>
        /// Get writable registry key
        /// </summary>
        /// <returns>An open registry key for this application's storage</returns>
        public static RegistryKey Settings()
        {
            string key = @"SOFTWARE\Dnskeeper\Settings";

            Registry.CurrentUser.CreateSubKey(key, true);

            return Registry.CurrentUser.OpenSubKey(key, true);
        }

        /// <summary>
        /// Get writable registry key
        /// </summary>
        /// <returns>An open registry key for this application's storage</returns>
        public static RegistryKey Profiles()
        {
            string key = @"SOFTWARE\Dnskeeper\Profiles";

            Registry.CurrentUser.CreateSubKey(key, true);

            return Registry.CurrentUser.OpenSubKey(key, true);
        }

        /// <summary>
        /// Determines whether an adapter is using DHCP on a specific IP protocol
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="ipv"></param>
        /// <returns>Returns true if using DHCP</returns>
        public static bool UsingDhcp(string adapter, int ipv)
        {
            string dhcpNeedle = "DNS servers configured through DHCP:";

            return Netsh($"interface ipv{ipv} show dns \"{adapter}\"").Contains(dhcpNeedle);
        }
    }
}
