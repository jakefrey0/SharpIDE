using System;
using Eto.Forms;
using Eto.Drawing;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace SharpIDE {
    
    public partial class NewProjectForm : Form {

        private CheckBox saveDpp;
        private TextBox dppTb,nameTb;
        private DropDown ddmenu=new DropDown();
        private MainForm sender;
        public NewProjectForm (MainForm sender) {

            Title=MainForm.name+": New Project";
            MinimumSize=new Size(400,200);
            BackgroundColor=Colors.DimGray;
            this.sender=sender;
            
            String projectsFolderPath,cmdPath=Environment.CurrentDirectory+"/list_templates";
            if (!File.Exists(sender.dpp)) {
                projectsFolderPath=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+'/'+MainForm.name+" Projects/";
                using (FileStream fs=File.Create(sender.dpp)) {
                    Byte[]buff=Encoding.ASCII.GetBytes(projectsFolderPath);
                    fs.Write(buff,0,buff.Length);
                }
            }
            else projectsFolderPath=File.ReadAllText(sender.dpp);

            if (!File.Exists(cmdPath)) {
                using (FileStream fs=File.Create(cmdPath)) {
                    Byte[]buff=Encoding.ASCII.GetBytes("dotnet new --list");
                    fs.Write(buff,0,buff.Length);
                }
            }

            const Int32 tbWidth=300;
            PixelLayout ps=new PixelLayout();
            ps.Add(new Label(){Text="Project name: ",TextColor=Colors.WhiteSmoke},new Point(1,8));
            nameTb=new TextBox() {Width=tbWidth,PlaceholderText="Project Name"};
            ps.Add(nameTb,new Point(145,1));
            ps.Add(new Label(){Text="Projects folder path: ",TextColor=Colors.WhiteSmoke},new Point(1,48));
            dppTb=new TextBox(){Text=projectsFolderPath,Width=tbWidth};
            ps.Add(dppTb,new Point(145,40));
            saveDpp=new CheckBox(){Text="Set default",Checked=false,TextColor=Colors.WhiteSmoke};
            ps.Add(saveDpp,new Point(450,46));
            
            String str=RunBatchOrBashFile(cmdPath);
            String[]sp=str.Split('\n');
            Int32 shortNameIdx=sp[2].IndexOf('S');
            ListItem li;
            foreach (String ln in sp.Skip(4)) {
                if (String.IsNullOrEmpty(ln)) break;
                li=new ListItem();
                li.Text=RemoveTrailingWhitespace(ln.Substring(0, shortNameIdx));
                li.Key=RemoveTrailingWhitespace(ln.Substring(shortNameIdx));
                ddmenu.Items.Add(li);
            }
            var query=ddmenu.Items.Where(x=>x.Text.StartsWith("Console App"));
            if (query.Any())
                ddmenu.SelectedIndex=ddmenu.Items.IndexOf(query.First());
            else
                ++ddmenu.SelectedIndex;
            ps.Add(new Label(){Text = "Project Type: ",TextColor=Colors.WhiteSmoke},1,88);
            ps.Add(ddmenu,145,80);
            Button btn=new Button(){ Text = "Create" };
            btn.Click+=CreateBtnClick;
            ps.Add(btn,1,128);
            Content=ps;

        }

        private void CreateBtnClick (Object s,EventArgs e) {

            if (saveDpp.Checked.Value)
                File.WriteAllText(sender.dpp,dppTb.Text);
            if (!Directory.Exists(dppTb.Text))
                Directory.CreateDirectory(dppTb.Text);
            String cpCmdDir=Environment.CurrentDirectory+"/create_project";
            WriteThenRun(cpCmdDir,"cd \""+dppTb.Text+"\"\ndotnet new sln -o \""+nameTb.Text+'"');
            String projDir=dppTb.Text+'/'+nameTb.Text+'/';
            String toLaunch=projDir+nameTb.Text+".sln";
            WriteThenRun(cpCmdDir,"cd \""+projDir+"\"\ndotnet new "+ddmenu.Items[ddmenu.SelectedIndex].Key+" -o \""+nameTb.Text+'"');
            WriteThenRun(cpCmdDir,"cd \""+dppTb.Text+"\"\ndotnet sln \""+toLaunch+"\" add \""+projDir+'/'+nameTb.Text+'/'+nameTb.Text+".csproj\"");
            if (File.Exists(toLaunch)) 
                sender.LaunchProject(toLaunch);
            else {
                toLaunch=dppTb.Text+nameTb.Text+'/'+nameTb.Text+".csproj";
                if (File.Exists(toLaunch))
                    sender.LaunchProject(toLaunch);
                else throw new Exception("Couldn't find project file path");
            }
            Close();

        }

        internal static String RunBatchOrBashFile (String path) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                //TODO::OSX
                return String.Empty;
            }
            Process proc=new Process();
            String fn=RuntimeInformation.IsOSPlatform(OSPlatform.Linux)?"/bin/bash":"cmd.exe";
            proc.StartInfo.RedirectStandardOutput=proc.StartInfo.CreateNoWindow=true;
            proc.StartInfo.FileName=fn;
            proc.StartInfo.Arguments='"'+path+'"';
            proc.StartInfo.UseShellExecute=false;
            proc.Start();
            String str=proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return str;
        }

        internal static String WriteThenRun (String path, String data) {
            File.WriteAllText(path,data);
            return RunBatchOrBashFile(path);
        }

        private static String RemoveTrailingWhitespace (String str) {
            StringBuilder sb=new StringBuilder();
            Char pc='?';
            foreach (Char c in str) {
                if (c==' ') {
                    if (pc==' ') {
                        return sb.ToString();
                    }
                }
                sb.Append(c);
                pc=c;
            }
            return sb.ToString();
        }
        
    }
    
}