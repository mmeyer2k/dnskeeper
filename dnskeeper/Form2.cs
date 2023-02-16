using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace dnskeeper
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/mmeyer2k/dnskeeper");
        }
    }
}
