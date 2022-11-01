using System;
using Eto.Forms;

namespace SharpIDE.Mac
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //UNDONE
            new Application(Eto.Platforms.Mac64).Run(new MainForm("~"));
        }
    }
}