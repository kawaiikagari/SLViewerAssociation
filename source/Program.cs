// SPDX-License-Identifier: GPL-3.0-or-later
// Distributed under GNU GPL version 3 or later, without warranty. See LICENSE.
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace SLViewerAssociation {
 static class Program {
  [STAThread]static int Main(string[] args){
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   try{
    var store=new SettingsStore();
    if(args.Length==0){Application.Run(new MainForm(store));return 0;}
    if(args.Length!=2||args[0]!="--open")throw new ArgumentException("起動引数が正しくありません。");
    string url=LinkLaunch.ValidateUrl(args[1]);var data=store.Load();var automatic=data.AutomaticViewer();
    if(automatic!=null)try{using(var p=LinkLaunch.Start(automatic,url)){}return 0;}catch(Exception e){MessageBox.Show("前回のビューアーで開けませんでした。別のビューアーを選んでください。\n\n"+e.Message,"SLViewerAssociation",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    Application.Run(new ChooserForm(store,url));return 0;
   }catch(Exception e){MessageBox.Show(e.Message,"SLViewerAssociation",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
  }
 }
 static class Ui {
  public static readonly Color Ink=Color.FromArgb(30,41,59),Blue=Color.FromArgb(37,99,235);
  public static void Setup(Form f,string title,Size size){f.Text=title;f.Font=new Font("Yu Gothic UI",10);f.ForeColor=Ink;f.BackColor=Color.FromArgb(247,249,252);f.AutoScaleMode=AutoScaleMode.Dpi;f.ClientSize=size;f.MinimumSize=new Size(size.Width+16,size.Height+39);f.StartPosition=FormStartPosition.CenterScreen;}
  public static TableLayoutPanel Root(Form f){var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24,18,24,18),ColumnCount=1};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));f.Controls.Add(p);return p;}
  public static void Add(TableLayoutPanel p,Control c,int height){int row=p.Controls.Count;p.RowStyles.Add(new RowStyle(height<0?SizeType.Percent:SizeType.Absolute,height<0?100:height));p.Controls.Add(c,0,row);}
  public static Label Label(string text,bool bold=false){return new Label{Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Yu Gothic UI",10,bold?FontStyle.Bold:FontStyle.Regular)};}
  public static Label Title(string text){return new Label{Text=text,Dock=DockStyle.Fill,Font=new Font("Yu Gothic UI",21,FontStyle.Bold)};}
  public static FlowLayoutPanel Flow(){return new FlowLayoutPanel{Dock=DockStyle.Fill,Margin=Padding.Empty,WrapContents=false};}
  public static Button Button(string text,EventHandler click,bool primary=false){var b=new Button{Text=text,AutoSize=true,Height=35,MinimumSize=new Size(80,35),Padding=new Padding(8,2,8,2),Margin=new Padding(0,0,7,0),FlatStyle=FlatStyle.Flat,BackColor=primary?Blue:Color.White,ForeColor=primary?Color.White:Ink,UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=primary?Blue:Color.FromArgb(203,213,225);b.Click+=click;return b;}
  public static ListView List(){var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,AccessibleName="登録済みビューアー一覧"};list.Columns.Add("登録名",180);list.Columns.Add("実行ファイル",440);list.Columns.Add("状態",120);return list;}
  public static ViewerProfile Selected(ListView list){return list.SelectedItems.Count==0?null:list.SelectedItems[0].Tag as ViewerProfile;}
  public static void Fill(ListView list,AppSettings data,string selected){list.BeginUpdate();list.Items.Clear();foreach(var v in data.Viewers){string state=!File.Exists(v.FilePath)?"見つかりません":v.Id==data.LastViewerId?(data.AskEveryTime?"前回使用":"既定"):"利用可能";var item=new ListViewItem(new[]{v.Name,v.FilePath,state}){Tag=v};if(!File.Exists(v.FilePath))item.ForeColor=Color.FromArgb(166,61,45);list.Items.Add(item);if(v.Id==selected)item.Selected=true;}if(list.SelectedItems.Count==0&&list.Items.Count>0)list.Items[0].Selected=true;list.EndUpdate();}
  public static void Error(IWin32Window owner,Exception e){MessageBox.Show(owner,e.Message,"SLViewerAssociation",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 }
 class MainForm:Form {
  readonly SettingsStore store;AppSettings data;readonly ListView viewers=Ui.List();readonly TextBox current=new TextBox();readonly Label status=Ui.Label("");
  readonly RadioButton ask=new RadioButton{Text="毎回ビューアーを選ぶ",AutoSize=true};readonly RadioButton remember=new RadioButton{Text="前回選んだビューアーを使う",AutoSize=true};readonly Button find;
  public MainForm(SettingsStore settings){
   store=settings;data=store.Load();Ui.Setup(this,"Second Life ビューアー関連付け設定",new Size(880,700));var root=Ui.Root(this);
   Ui.Add(root,Ui.Title("Second Life ビューアー設定"),50);Ui.Add(root,Ui.Label("登録したビューアーから切り替え。次回も使うか、毎回選ぶかを設定できます。"),32);
   Ui.Add(root,Ui.Label("現在の関連付け",true),25);current.ReadOnly=true;current.Dock=DockStyle.Top;current.AccessibleName="現在の関連付け";Ui.Add(root,current,36);
   var tools=Ui.Flow();find=Ui.Button("自動で探す",async delegate{await Search();});tools.Controls.Add(find);tools.Controls.Add(Ui.Button("手動で登録",delegate{Edit(false);}));tools.Controls.Add(Ui.Button("登録を編集",delegate{Edit(true);}));tools.Controls.Add(Ui.Button("一覧から削除",Remove));Ui.Add(root,tools,43);
   Ui.Add(root,viewers,-1);
   var mode=new GroupBox{Text="次回リンクを開くとき",Dock=DockStyle.Fill};var modes=Ui.Flow();modes.Padding=new Padding(10,6,0,0);modes.Controls.Add(ask);modes.Controls.Add(remember);remember.Margin=new Padding(25,3,3,3);mode.Controls.Add(modes);Ui.Add(root,mode,68);
   var actions=Ui.Flow();actions.Controls.Add(Ui.Button("選んだ設定を適用",Apply,true));actions.Controls.Add(Ui.Button("設定前に戻す",Restore));actions.Controls.Add(Ui.Button("Windowsの既定のアプリ",delegate{Run(Association.OpenDefaults);}));Ui.Add(root,actions,45);
   Ui.Add(root,status,43);var note=Ui.Label("リンクごとの選択画面でも「次回もこのビューアーを使う」を選べます。\n初回の適用後、Windowsの既定のアプリが優先される場合は設定を確認してください。");note.ForeColor=Color.FromArgb(71,85,105);Ui.Add(root,note,54);
   RefreshData(data.LastViewerId,true);Shown+=async delegate{if(data.Viewers.Count==0)await Search();};
   Activated+=delegate{Run(delegate{RefreshData(Ui.Selected(viewers)==null?null:Ui.Selected(viewers).Id,false);});};
  }
  void RefreshData(string selected,bool loadMode){data=store.Load();Ui.Fill(viewers,data,selected);if(loadMode){ask.Checked=data.AskEveryTime;remember.Checked=!data.AskEveryTime;}string effective=Association.CurrentViewer();if(effective.Equals(typeof(Association).Assembly.Location,StringComparison.OrdinalIgnoreCase)){var v=data.Viewers.FirstOrDefault(p=>p.Id==data.LastViewerId);current.Text=data.AskEveryTime?"SLViewerAssociation：毎回選択":("SLViewerAssociation："+(v==null?"次回選択":v.Name));}else current.Text=effective.Length==0?"（未設定）":effective;}
  async Task Search(){find.Enabled=false;status.Text="インストール情報と標準フォルダーを検索しています…";try{var found=await Task.Run(()=>Discovery.Find());data=store.Load();int added=data.Merge(found);if(added>0)store.Save(data);RefreshData(data.LastViewerId,false);status.Text=found.Count+"件見つかりました。新規登録 "+added+"件。"+(found.Count==0?"「手動で登録」から指定できます。":"");}catch(Exception e){Ui.Error(this,e);status.Text="検索を完了できませんでした。";}finally{find.Enabled=true;}}
  void Edit(bool editing){Run(delegate{var selected=Ui.Selected(viewers);if(editing&&selected==null)throw new ArgumentException("編集するビューアーを選択してください。");using(var dialog=new ProfileForm(editing?selected:null)){if(dialog.ShowDialog(this)!=DialogResult.OK)return;data=store.Load();var profile=data.Upsert(dialog.ProfileName,dialog.ViewerFile,editing?selected.Id:null);store.Save(data);RefreshData(profile.Id,false);status.Text=editing?"登録を更新しました。":"ビューアーを登録しました。";}});}
  void Remove(object sender,EventArgs e){Run(delegate{var selected=Ui.Selected(viewers);if(selected==null)throw new ArgumentException("一覧から削除するビューアーを選択してください。");data=store.Load();data.Remove(selected.Id);store.Save(data);RefreshData(data.LastViewerId,true);status.Text="登録一覧から削除しました。ビューアー本体は削除しません。";});}
  void Apply(object sender,EventArgs e){Run(delegate{var selected=Ui.Selected(viewers);data=store.Load();if(remember.Checked){if(selected==null)throw new ArgumentException("次回も使うビューアーを選択してください。");Association.Validate(selected.FilePath);data.LastViewerId=selected.Id;}if(data.Viewers.Count==0)throw new ArgumentException("ビューアーを登録してください。");data.AskEveryTime=ask.Checked;store.Save(data);Association.RegisterHandler();RefreshData(selected==null?null:selected.Id,true);status.Text=data.AskEveryTime?"適用しました。リンクを開くたびに選択画面を表示します。":"適用しました。選んだビューアーを次回から使用します。";});}
  void Restore(object sender,EventArgs e){Run(delegate{Association.Restore();RefreshData(data.LastViewerId,false);status.Text="関連付けを元に戻しました。登録一覧は保持しています。";});}
  void Run(Action action){try{action();}catch(Exception e){Ui.Error(this,e);}}
 }
 class ProfileForm:Form {
  readonly TextBox name=new TextBox(),file=new TextBox();public string ProfileName{get{return name.Text;}}public string ViewerFile{get{return file.Text;}}
  public ProfileForm(ViewerProfile profile){Ui.Setup(this,profile==null?"ビューアーを登録":"登録を編集",new Size(680,280));ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;var root=Ui.Root(this);
   Ui.Add(root,Ui.Label("登録名",true),30);name.Dock=DockStyle.Top;name.MaxLength=120;name.AccessibleName="ビューアー登録名";Ui.Add(root,name,38);Ui.Add(root,Ui.Label("ビューアーの実行ファイル（.exe）",true),30);
   var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));file.Dock=DockStyle.Top;file.AccessibleName="ビューアー実行ファイル";row.Controls.Add(file,0,0);row.Controls.Add(Ui.Button("参照…",Browse),1,0);Ui.Add(root,row,45);
   var actions=Ui.Flow();var save=Ui.Button("登録を保存",delegate{try{Association.Validate(file.Text);if(string.IsNullOrWhiteSpace(name.Text))throw new ArgumentException("登録名を入力してください。");DialogResult=DialogResult.OK;}catch(Exception e){Ui.Error(this,e);}},true);actions.Controls.Add(save);var cancel=Ui.Button("キャンセル",delegate{DialogResult=DialogResult.Cancel;});actions.Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;Ui.Add(root,actions,45);
   if(profile!=null){name.Text=profile.Name;file.Text=profile.FilePath;}
  }
  void Browse(object sender,EventArgs e){using(var d=new OpenFileDialog{Filter="実行ファイル (*.exe)|*.exe",CheckFileExists=true,Title="ビューアー本体を選択"})if(d.ShowDialog(this)==DialogResult.OK){file.Text=d.FileName;if(string.IsNullOrWhiteSpace(name.Text))name.Text=Path.GetFileNameWithoutExtension(d.FileName);}}
 }
 class ChooserForm:Form {
  readonly SettingsStore store;AppSettings data;readonly string url;readonly ListView viewers=Ui.List();readonly CheckBox remember=new CheckBox{Text="次回もこのビューアーを使う（選択画面を省略）",AutoSize=true};readonly Label status=Ui.Label("");
  public ChooserForm(SettingsStore settings,string target){store=settings;url=LinkLaunch.ValidateUrl(target);data=store.Load();Ui.Setup(this,"Second Life — 開くビューアーを選択",new Size(820,490));var root=Ui.Root(this);
   Ui.Add(root,Ui.Title("どのビューアーで開きますか？"),52);var link=new TextBox{Text=url,ReadOnly=true,Dock=DockStyle.Top,AccessibleName="開くSecond Lifeリンク"};Ui.Add(root,link,36);Ui.Add(root,viewers,-1);Ui.Add(root,remember,39);
   var actions=Ui.Flow();var open=Ui.Button("選んだビューアーで開く",Open,true);actions.Controls.Add(open);actions.Controls.Add(Ui.Button("登録・設定",Manage));var cancel=Ui.Button("キャンセル",delegate{Close();});actions.Controls.Add(cancel);CancelButton=cancel;Ui.Add(root,actions,44);Ui.Add(root,status,40);Ui.Add(root,Ui.Label("チェックしなければ、次回もここで選べます。設定画面からもいつでも変更できます。"),30);
   Ui.Fill(viewers,data,data.LastViewerId);viewers.DoubleClick+=Open;
   Shown+=async delegate{if(data.Viewers.Count==0){status.Text="ビューアーを自動検索しています…";try{var found=await Task.Run(()=>Discovery.Find());data=store.Load();int n=data.Merge(found);if(n>0)store.Save(data);Ui.Fill(viewers,data,data.LastViewerId);status.Text=n==0?"ビューアーが見つかりません。「登録・設定」から追加してください。":n+"件のビューアーを登録しました。";}catch(Exception e){Ui.Error(this,e);}}};
  }
  void Manage(object sender,EventArgs e){try{using(var form=new MainForm(store))form.ShowDialog(this);data=store.Load();Ui.Fill(viewers,data,data.LastViewerId);}catch(Exception ex){Ui.Error(this,ex);}}
  void Open(object sender,EventArgs e){try{var selected=Ui.Selected(viewers);if(selected==null)throw new ArgumentException("開くビューアーを選択してください。");data=store.Load();var actual=data.Viewers.FirstOrDefault(v=>v.Id==selected.Id);using(var process=LinkLaunch.Start(actual,url)){}LinkLaunch.Remember(data,actual,remember.Checked);try{store.Save(data);}catch(Exception saveError){MessageBox.Show(this,"ビューアーを起動しましたが、次回の設定を保存できませんでした。\n\n"+saveError.Message,"SLViewerAssociation",MessageBoxButtons.OK,MessageBoxIcon.Warning);}Close();}catch(Exception ex){Ui.Error(this,ex);}}
 }
}
