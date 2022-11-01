using System;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using System.Linq;
using System.Reflection;

namespace SharpIDE {
	public partial class MainForm : Form {
		
		public const String name="SharpIDE";

		private TreeGridView filesTreeView;
		private TabControl projectTabCtrl;
        internal String dpp=Environment.CurrentDirectory+"/default_project_path";
        private String loadedProjectPath;

		public MainForm(String projectPath) {

			Title=name+" C# Developer Environment";
			MinimumSize = new Size(200, 200);

			Command load=new Command(){MenuText="Load project (Ctrl+L)",ToolBarText="Load existing project",Shortcut=Application.Instance.CommonModifier|Keys.L},
				    @new=new Command(){MenuText="New project (Ctrl+N)",ToolBarText="Create project",Shortcut=Application.Instance.CommonModifier|Keys.N},
				    quit=new Command(){MenuText="Quit (Ctrl+Q)",Shortcut=Application.Instance.CommonModifier|Keys.Q};;
			load.Executed+=(s,e)=>LoadProject();
			@new.Executed+=(s,e)=>NewProject();
			quit.Executed+=(sender,e)=>Application.Instance.Quit();
			Menu = new MenuBar {
				Items =
				{
					// File submenu
					// new SubMenuItem { Text = "&File", Items = { load,@new } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
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

		private void NewProject () { new NewProjectForm(this).Show(); }

		public void LaunchProject (String projectSlnOrCsprojPath) {
			Title="(EasySharp: "+projectSlnOrCsprojPath.Split(new []{'/','\\'}).Last().Split('.').First()+')';
			ToolBar=new ToolBar() { Items = { } };
			PixelLayout ps=new PixelLayout();
			TreeGridItemCollection col=new TreeGridItemCollection();
			filesTreeView=new TreeGridView(){Width=300,Height=this.Height-200};
			filesTreeView.Columns.Add(new GridColumn(){DataCell=new ImageViewCell(0){},Editable=false,Resizable=false,Sortable=false,Width=34});
			filesTreeView.Columns.Add(new GridColumn(){DataCell=new TextBoxCell(1),HeaderText="File Name",Editable=false,Resizable=false,Sortable=false});
			Assembly asm=Assembly.GetExecutingAssembly();
			String dirName=Path.GetDirectoryName(projectSlnOrCsprojPath),asmName=asm.GetName().Name;
			FillTreeView(dirName,asmName,col,asm);
			filesTreeView.DataStore=col;
			ps.Add(filesTreeView,1,33);
			projectTabCtrl=new TabControl(){Size=new Size(this.Width-500,this.Height-200)};
			ps.Add(projectTabCtrl,304,33);
			loadedProjectPath=Path.GetDirectoryName(projectSlnOrCsprojPath);
			filesTreeView.SelectionChanged+=delegate { 
				if (filesTreeView.SelectedItem==null)
					filesTreeView.SelectedItem=col.Last(); // this is the fault of eto, it does not make any sense that the selection could have been changed yet is null but it happens anyway. This is a fix though it is ugly
				OpenFileWithinProject(((TreeGridItem)filesTreeView.SelectedItem).Tag.ToString()); 
			};
			
			filesTreeView.CellClick+=delegate(object sender, GridCellMouseEventArgs args) {
				if (args.Buttons!=MouseButtons.Alternate) return;
				// Right click:
				Command delete=new Command(){MenuText="Delete file"};
				delete.Executed+=delegate {
					if (MessageBox.Show("Are you sure you want to delete this file? It may not be recoverable.",MessageBoxButtons.YesNo)!=DialogResult.Yes)
						return; // < Pressed no or exited out some other way (X button? Alt+f4?)
					// User pressed yes:
					try {
						String tag=((TreeGridItem)args.Item).Tag.ToString();
						File.Delete(tag);
						
						col.Remove((TreeGridItem)args.Item);
						filesTreeView.ReloadData();
						if (projectTabCtrl.Pages.Any(x=>x.ID==tag))
							CloseFileWithinProject(tag);
					}
					catch (IOException) { MessageBox.Show("The file is in use.","Failure"); }
					catch (UnauthorizedAccessException) { MessageBox.Show(name+" does not have permission to remove this file","Failure" ); }
					catch (Exception ex) { MessageBox.Show(ex.Message,"Failure"); }
					
				};
				
				new ContextMenu() {Items={delete}}.Show();
				
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
			ps.Add(rtb,2,52);
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
			page.Content=ps;
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
		
	}
	
}
