using System;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace dnskeeper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                string msg = "This application requires administrator permissions.\n\nWould you like to relaunch dnskeeper as admin?";

                DialogResult result = MessageBox.Show(msg, "Dnskeeper", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    StartAsAdmin(Assembly.GetExecutingAssembly().Location);
                }

                Environment.Exit(0);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        /// <summary>
        /// Check if running as administrator
        /// </summary>
        /// <returns>Returns true if running as admin</returns>
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        /// <summary>
        /// Restart app as administrator
        /// </summary>
        /// <param name="exepath"></param>
        public static void StartAsAdmin(string exepath)
        {
            var proc = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = exepath,
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };

            proc.Start();
        }
    }
}
