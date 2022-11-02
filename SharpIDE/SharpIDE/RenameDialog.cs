using Eto.Forms;
using System;
using System.IO;
using System.Linq;
using Eto.Drawing;

namespace SharpIDE {

    public class RenameDialog : Dialog {
        
        public RenameDialog (String path) {
            
            String friendlyName=path.Split(new[]{'/','\\'}).Last();
            Title=MainForm.name+": Rename "+friendlyName;
            Width=303;
            Height=34;
            Padding=0;
            Resizable=false;
            
            PixelLayout pl=new PixelLayout();
            TextBox tb=new TextBox(){Width=200,Height=30,Text=friendlyName};
            pl.Add(tb,2,2);
            Button btn=new Button(){Width=46,Height=30,Text="OK"};
            btn.Click+=delegate {
                try {
                    File.Move(path,path.Substring(0,path.Length-friendlyName.Length)+tb.Text);
                    Close();
                }
                catch (Exception e) { MessageBox.Show(e.Message,"Failure");}
            };
            pl.Add(btn,202,2);
            Button btn0=new Button(){Width=46,Height=30,Text="Exit"};
            btn0.Click+=(x,y)=>Close();
            pl.Add(btn0,252,2);
            Content=pl;
                
        }
        
        
    }
    
}