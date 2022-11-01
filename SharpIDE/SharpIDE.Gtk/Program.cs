using System;
using Eto.Forms;
using System.IO;

namespace SharpIDE.Gtk {
    class Program {
        [STAThread]
        public static void Main(String[]args) {
            String projectPath=null;
            if (args.Length==1) {
                projectPath=args[0];
                if (!File.Exists(projectPath)) {
                    Console.WriteLine("Invalid path: "+projectPath);
                    Environment.Exit(0);
                }
            }
            else if (args.Length!=0) {
                Console.WriteLine("Expected 1 or 0 arguments");
                Environment.Exit(0);
            }
            Eto.Platform plat=new Eto.GtkSharp.Platform();
            new Application(plat).Run(new MainForm(projectPath));
        }
    }
}