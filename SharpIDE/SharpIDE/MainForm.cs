using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Eto.Forms.Controls.Scintilla.Shared;
using Gtk;
using Application = Eto.Forms.Application;
using Button = Eto.Forms.Button;
using Char = System.Char;
using Clipboard = Gtk.Clipboard;
using MenuBar = Eto.Forms.MenuBar;
using Object = System.Object;
using Size = Eto.Drawing.Size;
using System.Globalization;

namespace SharpIDE {
	public partial class MainForm : Form {
	
		// TODO:: undone commands, make scintilla actually edit files
		
		public const String name="SharpIDE";

		private TreeGridView filesTreeView;
		private TabControl projectTabCtrl;
        internal String dpp=Environment.CurrentDirectory+"/default_project_path",editorSettingsFp=Environment.CurrentDirectory+"/texteditor_settings";
        private String loadedProjectPath;

		public MainForm(String projectPath) {

			Title=name+" C# Developer Environment";
			MinimumSize = new Size(200, 200);

			Command load=new Command(){MenuText="Load project",ToolBarText="Load existing project",Shortcut=Application.Instance.CommonModifier|Keys.L},
				    @new=new Command(){MenuText="New project",ToolBarText="Create project",Shortcut=Application.Instance.CommonModifier|Keys.N},
				    quit=new Command(){MenuText="Quit",Shortcut=Application.Instance.CommonModifier|Keys.Q},
				    save=new Command(){MenuText="Save",Shortcut=Application.Instance.CommonModifier|Keys.S},
				    saveAll=new Command(){MenuText="Save All",Shortcut=Application.Instance.CommonModifier|Keys.Shift|Keys.S};
			load.Executed+=(s,e)=>LoadProject();
			@new.Executed+=(s,e)=>NewProject();
			quit.Executed+=(sender,e)=>Application.Instance.Quit();
			Command modifyTextEditor=new Command(){MenuText="Text Editor Settings",ToolBarText="Text Editor Settings"};
			Command findAndReplace=new Command(){MenuText="Find and Replace",ToolBarText="Find and Replace",Shortcut=Application.Instance.CommonModifier|Keys.F};
			modifyTextEditor.Executed+=(x,y)=>new TextEditorSettingsDialog().ShowModal();
			findAndReplace.Executed+=(x,y)=>new FindAndReplaceDialog().ShowModal();
			Command undo=new Command(){MenuText="Undo",ToolBarText="Undo",Shortcut=Application.Instance.CommonModifier|Keys.Z};
			Command redo=new Command(){MenuText="Redo",ToolBarText="Redo",Shortcut=Application.Instance.CommonModifier|Keys.Y};
			Command build=new Command(){MenuText="Build Solution",ToolBarText="Build Solution",Shortcut=Keys.F6};
			Command debug=new Command(){MenuText="Debug",ToolBarText="Debug",Shortcut=Keys.F7};
			Command buildSettings=new Command(){MenuText="Build Settings",ToolBarText="Build Settings"};
			
			Menu = new MenuBar {
				Items =
				{
					// File submenu
					new SubMenuItem {Text="&File",Items={load,@new,save,saveAll}},
					new SubMenuItem {Text="&Edit",Items={undo,redo}},
					new SubMenuItem {Text="&View",Items={modifyTextEditor,findAndReplace}},
					new SubMenuItem {Text="&Build",Items={build,debug,buildSettings}},
				},
				QuitItem = quit,
			};
				
			ToolBar = new ToolBar { Items = { load,@new } };
			BackgroundColor=Colors.FloralWhite;

		}

		private void LoadProject (String projectPath=null) {
			if (String.IsNullOrEmpty(projectPath)) {
				OpenFileDialog ofd=new OpenFileDialog();
				ofd.MultiSelect=false;
				ofd.Filters.Add("C# Project or Solution|*.csproj;*.sln");
				ofd.Filters.Add("C# Project|*.csproj");
				ofd.Filters.Add("Solution File|*.sln");
				ofd.Filters.Add("Any|*.*");
				if (File.Exists(dpp)) {
					String dppTxt=File.ReadAllText(dpp);
					if (Directory.Exists(dppTxt))
						ofd.Directory=new Uri(dppTxt);
				}
				if (ofd.ShowDialog(this)!=DialogResult.Ok)
					return;
				projectPath=ofd.FileName;
			}
			LaunchProject(projectPath);
			
		}

		private void NewProject () { new NewProjectDialog(this).ShowModal(); }

		public void LaunchProject (String projectSlnOrCsprojPath) {
			Title="(EasySharp: "+projectSlnOrCsprojPath.Split(new []{'/','\\'}).Last().Split('.').First()+')';
			ToolBar=new ToolBar() { Items = { } };
			PixelLayout ps=new PixelLayout();
			projectTabCtrl=new TabControl(){Size=new Size(this.Width-500,this.Height-200)};
			ps.Add(projectTabCtrl,304,33);
			TreeGridItemCollection col=new TreeGridItemCollection();
			filesTreeView=new TreeGridView(){Width=300,Height=this.Height-200};
			filesTreeView.Columns.Add(new GridColumn(){DataCell=new ImageViewCell(0){},Editable=false,Resizable=false});
			filesTreeView.Columns.Add(new GridColumn(){DataCell=new TextBoxCell(1),HeaderText="File Name",Editable=false,Resizable=false});
			Assembly asm=Assembly.GetExecutingAssembly();
			String dirName=Path.GetDirectoryName(projectSlnOrCsprojPath),asmName=asm.GetName().Name;
			FillTreeView(dirName,asmName,col,asm);
			filesTreeView.DataStore=col;
			ps.Add(filesTreeView,1,33);
			loadedProjectPath=Path.GetDirectoryName(projectSlnOrCsprojPath);
			
			filesTreeView.CellClick+=delegate(object sender, GridCellMouseEventArgs args) {
				if (args.Buttons==MouseButtons.Primary) {
					OpenFileWithinProject(((TreeGridItem)args.Item).Tag.ToString()); 
					return;
				}
				if (args.Buttons!=MouseButtons.Alternate) return;
				String tag=((TreeGridItem)args.Item).Tag.ToString();
				// Right click:
				Command delete=new Command(){MenuText="Delete"};
				delete.Executed+=delegate {
					if (MessageBox.Show("Are you sure you want to delete this file or directory? It may not be recoverable.",MessageBoxButtons.YesNo)!=DialogResult.Yes)
						return; // < Pressed no or exited out some other way (X button? Alt+f4?)
					// User pressed yes:
					try {
						if (!File.Exists(tag)) {
							if (Directory.Exists(tag))
								Directory.Delete(tag,true);
							else if (MessageBox.Show("This file/directory appears to not exist. Should it be removed from the list?",MessageBoxButtons.YesNo)!=DialogResult.Yes)
								return;
						}
						else File.Delete(tag);
						TreeGridItem item=(TreeGridItem)args.Item;
						if (item.Parent==null) col.Remove(item);
						else ((TreeGridItem)item.Parent).Children.Remove((TreeGridItem)args.Item);
						filesTreeView.ReloadData();
						if (projectTabCtrl.Pages.Any(x=>x.ID==tag))
							CloseFileWithinProject(tag);
					}
					catch (IOException) { MessageBox.Show("The file/directory is in use.","Failure"); }
					catch (UnauthorizedAccessException) { MessageBox.Show(name+" does not have permission to remove this file/directory","Failure" ); }
					catch (Exception ex) { MessageBox.Show(ex.Message,"Failure"); }
					
				};
				
				Command rename=new Command(){MenuText="Rename"};
				rename.Executed+=delegate {
					RenameDialog rd=new RenameDialog(tag);
					rd.ShowModal();
					if (!File.Exists(tag))
					{
						// remove and update with renamed one, make sure it goes under correct parent
					}
				};
				
				Command copy=new Command(){MenuText="Copy"};
				copy.Executed+=delegate {
					if (!File.Exists(tag)&&!Directory.Exists(tag))
						MessageBox.Show("This file or directory does not appear to exist");
					Clipboard.Get(Gdk.Selection.Clipboard).SetWithData(new []{new TargetEntry("x-special/gnome-copied-files",0,0),new TargetEntry("text/uri-list",0,0)},delegate(Clipboard clipboard,SelectionData data,UInt32 info) {
						data.Set(data.Target,8,Encoding.ASCII.GetBytes("copy\n"+new Uri(tag).ToString()));
					},null);
				};
				
				Command edit=new Command(){MenuText="Edit"};
				edit.Executed+=delegate {
					if (!File.Exists(tag))
						MessageBox.Show("This file does not appear to exist","Failure");
					else
						OpenFileWithinProject(tag);
				};
				
				Command openInMgr=new Command(){MenuText="Open in file manager"};
				openInMgr.Executed+=delegate {
					if (!File.Exists(tag)&&!Directory.Exists(tag))
						MessageBox.Show("This file or directory does not appear to exist");
					else NewProjectDialog.WriteThenRun(Environment.CurrentDirectory+"/fmgr_script","xdg-open \""+Path.GetDirectoryName(tag)+'"');
				};
				
				Command openExternal=new Command(){MenuText="Open externally"};
				openExternal.Executed+=delegate {
					if (!File.Exists(tag))
						MessageBox.Show("This file or directory does not appear to exist");
					else NewProjectDialog.WriteThenRun(Environment.CurrentDirectory+"/fmgr_script","xdg-open \""+tag+'"');
				};
				
				if (Directory.Exists(tag))
					new ContextMenu() {Items={rename,copy,delete,openInMgr}}.Show();
				else
					new ContextMenu() {Items={edit,rename,copy,delete,openExternal,openInMgr}}.Show();
				
			};
			
			Content=ps;
				
		}
	
		private void FillTreeView (String dirName,String asmName,TreeGridItemCollection col,Assembly asm,Boolean expand=true) {
			foreach (String s in Directory.GetDirectories(dirName).Select(x=>x.Split(new []{'/','\\'}).Last())) {
				TreeGridItem item=new TreeGridItem(2){Values=new Object[]{new Bitmap(asm.GetManifestResourceStream(asmName+".Resources.Folder.bmp")),s},Expanded=expand,Tag=dirName+'/'+s};
				col.Add(item);
				FillTreeView(dirName+'/'+s,asmName,item.Children,asm,false);
			}
			foreach (String s in Directory.GetFiles(dirName).Select(x=>x.Split(new []{'/','\\'}).Last())) {
				String imgFn=".Resources.file-text-outline.bmp";
				if (s.Contains('.')) {
					if (s.EndsWith(".cs"))
						imgFn=".Resources.c-sharp-c.bmp";
					else if (s.EndsWith(".sln")||s.EndsWith(".csproj"))
						imgFn=".Resources.project-diagram-solid.bmp";
				}
				TreeGridItem item=new TreeGridItem(){Values=new Object[]{new Bitmap(asm.GetManifestResourceStream(asmName+imgFn)),s},Tag=dirName+'/'+s};
				col.Add(item);
				
			}
		}
		
		private void OpenFileWithinProject (String fileName) {
			if (!File.Exists(fileName))
				return; // User opened folder which can't be displayed as a file
			if (projectTabCtrl.Controls.Any(x=>x.ID==fileName)) {
				// Tab is already open, only focus it
				projectTabCtrl.SelectedIndex=projectTabCtrl.Controls.ToList().IndexOf(projectTabCtrl.Controls.First(x=>x.ID==fileName));
				return;
			}
			TabPage page=new TabPage();
			page.ID=fileName;
			page.Text=fileName.Split(new Char[]{'\\','/'}).Last();
			PixelLayout ps=new PixelLayout();
			RichTextArea rtb=new RichTextArea(){Width=projectTabCtrl.Width-8,Height=projectTabCtrl.Height-100};
			ScintillaControl sc=new ScintillaControl(){Width=projectTabCtrl.Width-8,Height=projectTabCtrl.Height-100};
            LoadScintillaSettings(sc);
			
            ps.Add(sc,2,52);
			//TODO:: Move these buttons to a contextmenu on tab right click, Eto doesn't have enough support right now to accomplish this
			Button closeBtn=new Button(){Text="Close this tab"},closeToLeftBtn=new Button(){Text="Close tabs to the left"},closeToRightBtn=new Button(){Text="Close tabs to the right"},closeAllButThisBtn=new Button(){Text="Close all other tabs"};
			closeBtn.Click+=delegate { CloseFileWithinProject(projectTabCtrl.SelectedIndex); };
			closeToLeftBtn.Click+=delegate { CloseFilesWithinProjectToLeft(); };
			closeToRightBtn.Click+=delegate { CloseFilesWithinProjectToRight(); };
			closeAllButThisBtn.Click+=delegate {
				CloseFilesWithinProjectToRight();
				CloseFilesWithinProjectToLeft();
			};
			Int32 prefWidth=GetPrefWidth(closeBtn),prefWidth0=GetPrefWidth(closeToLeftBtn),prefWidth1=GetPrefWidth(closeToRightBtn);//,prefWidth2=GetPrefWidth(closeAllButThisBtn);
			ps.Add(closeBtn,2,2);
			ps.Add(closeToLeftBtn,4+prefWidth,2);
			ps.Add(closeToRightBtn,6+prefWidth+prefWidth0,2);
			ps.Add(closeAllButThisBtn,8+prefWidth+prefWidth0+prefWidth1,2);
			page.Content=sc;
			projectTabCtrl.Pages.Add(page);
			projectTabCtrl.SelectedIndex=projectTabCtrl.Pages.Count-1;
		}
		private void CloseFileWithinProject (String fileName) {
			projectTabCtrl.Pages.Remove(projectTabCtrl.Pages.First(x=>x.ID==fileName));
		}
		private void CloseFileWithinProject (Int32 pageIndex) {
			projectTabCtrl.Pages.RemoveAt(pageIndex);
		}
		private void CloseFilesWithinProjectToRight () {
			Int32 i=projectTabCtrl.Pages.Count;
			while (--i!=projectTabCtrl.SelectedIndex)
				CloseFileWithinProject(i);
		}
		private void CloseFilesWithinProjectToLeft () {
			Int32 i=0,startIndex=projectTabCtrl.SelectedIndex;
			while (i!=startIndex) {
				CloseFileWithinProject(i);
				++i;
			}
		}
		
		private Int32 GetPrefWidth (Control ctrl) { return (Int32)ctrl.GetPreferredSize().Width; }
		
		private void LoadScintillaSettings (ScintillaControl sc) {
			if (!File.Exists(editorSettingsFp)) {
				using (FileStream fs=File.Create(editorSettingsFp))	{
					const String defaultSettings=@"To modify, go to SharpIDE->View->Text Editor Settings			
12
highlight 5 0000FF
highlight 16 FF0000
highlight 6 008CFF
highlight 4 F09300
highlight 1 909090
highlight 2 909090
highlight 3 909090
font DejaVu Sans Mono					
					";
					fs.Write(Encoding.ASCII.GetBytes(defaultSettings),0,defaultSettings.Length);
				}
			}
			
			String[]lns=File.ReadAllLines(editorSettingsFp);
			
			sc.SetFontSize(Int32.Parse(lns[1]));
			sc.ClearAllStyles();
            sc.SetKeywords(0, "abstract as base break case catch checked continue default delegate do else event explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null object operator out override params private protected public readonly ref return sealed sizeof stackalloc switch this throw true try typeof unchecked unsafe using virtual while");
			sc.SetKeywords(1, "bool byte char class const decimal double enum float int long sbyte short static string struct uint ulong ushort void");		
			
			foreach (String str in lns.Skip(2)) {
				String[]sp=str.Split(' ');
				switch (sp[0]) {
					case "highlight":
						sc.SetParameter(Constants.SCI_STYLESETFORE,new IntPtr(Int32.Parse(sp[1])),new IntPtr(Int32.Parse(sp[2],NumberStyles.HexNumber)));
						break;
					case "font":
						sc.SetParameter(Constants.SCI_STYLESETFONT,Constants.STYLE_DEFAULT.ToIntPtr(),str.Substring(sp[0].Length+1).ToIntPtr());
						break;
				}
			}
            
           
		}
		
	}
	
}
