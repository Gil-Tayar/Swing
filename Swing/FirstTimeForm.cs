using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Swing
{
    public partial class FirstTimeForm : Form
    {
        // consts
        const string GITHUB_PAGE = "https://github.com/Gil-Tayar/Swing";

        public FirstTimeForm()
        {
            InitializeComponent();
        }

        private void LinkGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkGithub.LinkVisited = true;

            // Navigate to a URL.
            System.Diagnostics.Process.Start(GITHUB_PAGE);
        }
    }
}
