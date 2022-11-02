using Eto.Forms;
using System;
using System.IO;
using System.Linq;
using Eto.Drawing;

namespace SharpIDE {

    public class TextEditorSettingsDialog : Dialog {
        
        public TextEditorSettingsDialog () {
            
            // Maybe add a button per tabpage that says "Refresh Editor Settings" that will entirely reload the scintilla
            // And have this only modify the text editor settings file data
            // That way the task bar items like the text editor settings one can be global and a project doesn't have to be opened
            
            Title=MainForm.name+": Text Editor Settings";
            Width=400;
            Height=300;
            Padding=0;
            Resizable=false;
            
            PixelLayout pl=new PixelLayout();
            
            
            
            Content=pl;
                
        }
        
        
    }
    
}