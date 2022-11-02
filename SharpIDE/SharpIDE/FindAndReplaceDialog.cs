using Eto.Forms;
using System;
using System.IO;
using System.Linq;
using Eto.Drawing;

namespace SharpIDE {

    public class FindAndReplaceDialog : Dialog {
        
        public FindAndReplaceDialog () {
            
            // Require a scintilla to be open on the current tab page for this to be opened
            
            Title=MainForm.name+": Find and Replace";
            Width=400;
            Height=300;
            Padding=0;
            Resizable=false;
            
            PixelLayout pl=new PixelLayout();
            
            
            
            Content=pl;
                
        }
        
        
    }
    
}