using System;
using Eto.Forms;
using System.IO;
using Eto.Forms.Controls.Scintilla.Shared;
using Eto.Forms.Controls.Scintilla.GTK;

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
            Eto.GtkSharp.Platform plat=new Eto.GtkSharp.Platform();
            plat.Add<ScintillaControl.IScintillaControl>(()=>new ScintillaControlHandler());
            new Application(plat).Run(new MainForm(projectPath));
        }
    }
}