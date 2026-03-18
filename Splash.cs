using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MissionPlanner
{
    public partial class Splash : Form
    {
        internal static Splash instance;

        public Splash()
        {
            InitializeComponent();

            string strVersion = typeof(Splash).GetType().Assembly.GetName().Version.ToString();

            TXT_version.Text = "Version: " + Application.ProductVersion; // +" Build " + strVersion;

            Console.WriteLine(strVersion);

            if (Program.Logo != null)
            {
                pictureBox1.BackgroundImage = MissionPlanner.Properties.Resources.bgdark;
                pictureBox1.Image = Program.Logo2;
                pictureBox1.Visible = true;
            }

            Console.WriteLine("Splash .ctor");
        }
    }
}