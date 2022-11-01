using System;
using Eto.Forms;

namespace SharpIDE.Wpf
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //UNDONE
            new Application(Eto.Platforms.Wpf).Run(new MainForm("~"));
        }
    }
}