// SPDX-License-Identifier: GPL-3.0-or-later
// SLViewerAssociation — Second Life viewer association settings.
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See LICENSE.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace SLViewerAssociation {
 static class Program {
  [STAThread]static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());}
 }
 class MainForm:Form {
  readonly ComboBox viewer=new ComboBox();readonly TextBox current=new TextBox();readonly Label status=new Label();
  static readonly Color Blue=Color.FromArgb(37,99,235),Ink=Color.FromArgb(30,41,59);
  public MainForm(){
   Text="Second Life ビューアー関連付け設定";Font=new Font("Yu Gothic UI",10);ForeColor=Ink;
   AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(760,450);MinimumSize=new Size(776,489);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(247,249,252);
   var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(26,20,26,16),ColumnCount=1,RowCount=8};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(root);
   Add(root,new Label{Text="Second Life ビューアー設定",AutoSize=true,Font=new Font(Font.FontFamily,21,FontStyle.Bold)},53);
   Add(root,TextLabel("リンクを開くビューアーを指定します。場所URLは毎回変わっても、そのまま開けます。"),45);
   Add(root,TextLabel("現在の関連付け",true),27);
   current.ReadOnly=true;current.Dock=DockStyle.Top;current.AccessibleName="現在の関連付け";Add(root,current,40);
   Add(root,TextLabel("使用するビューアー（.exe）",true),29);
   var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,102));
   viewer.Dock=DockStyle.Top;viewer.AccessibleName="使用するビューアー";viewer.AutoCompleteMode=AutoCompleteMode.SuggestAppend;viewer.AutoCompleteSource=AutoCompleteSource.ListItems;
   row.Controls.Add(viewer,0,0);row.Controls.Add(MakeButton("参照…",Browse,false),1,0);Add(root,row,47);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,Margin=Padding.Empty,WrapContents=false};
   actions.Controls.Add(MakeButton("このビューアーに設定",Apply,true));
   actions.Controls.Add(MakeButton("設定前に戻す",Restore,false));
   actions.Controls.Add(MakeButton("Windowsの既定のアプリ",delegate{Run(Association.OpenDefaults);},false));Add(root,actions,49);
   var bottom=new Panel{Dock=DockStyle.Fill};status.Dock=DockStyle.Top;status.Height=42;bottom.Controls.Add(status);
   var note=TextLabel("設定後は普段通りリンクを開いてください。地図ページでは「Teleport」を押します。\n変更が反映されない場合は、Windowsの既定のアプリで SECOND LIFE の関連付けを\n「Second Life ビューアー（選択済み）」に変更してください。");note.ForeColor=Color.FromArgb(71,85,105);note.Dock=DockStyle.Bottom;note.Height=68;bottom.Controls.Add(note);Add(root,bottom,0);root.RowStyles[root.RowStyles.Count-1]=new RowStyle(SizeType.Percent,100);
   Run(delegate{RefreshCurrent();Detect();if(File.Exists(current.Text))viewer.Text=current.Text;});
  }
  void Add(TableLayoutPanel p,Control c,int height){int row=p.Controls.Count;p.RowStyles.Add(new RowStyle(SizeType.Absolute,height));p.Controls.Add(c,0,row);}
  Label TextLabel(string text,bool bold=false){return new Label{Text=text,Dock=DockStyle.Fill,Font=new Font(Font,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};}
  Button MakeButton(string text,EventHandler handler,bool primary){
   var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(90,35),Height=35,Padding=new Padding(9,2,9,2),Margin=new Padding(0,0,8,0),FlatStyle=FlatStyle.Flat,BackColor=primary?Blue:Color.White,ForeColor=primary?Color.White:Ink,UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=primary?Blue:Color.FromArgb(203,213,225);b.Click+=handler;return b;
  }
  void RefreshCurrent(){string file=Association.CurrentViewer();current.Text=file.Length==0?"（未設定）":file;}
  void Browse(object sender,EventArgs e){using(var d=new OpenFileDialog{Filter="実行ファイル (*.exe)|*.exe",Title="Second Lifeのビューアーを選択",CheckFileExists=true})if(d.ShowDialog(this)==DialogResult.OK)viewer.Text=d.FileName;}
  void Apply(object sender,EventArgs e){Run(delegate{Association.Apply(viewer.Text);RefreshCurrent();status.Text="関連付けを設定しました。以後のリンクに適用されます。";});}
  void Restore(object sender,EventArgs e){Run(delegate{Association.Restore();RefreshCurrent();status.Text="設定前に戻しました。Windows側でも既定のアプリを変更した場合は戻してください。";});}
  void Run(Action action){try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"ビューアー関連付け設定",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  void Detect(){
   foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}.Distinct())try{
    foreach(string folder in Directory.GetDirectories(root).Where(d=>new[]{"secondlife","second life","firestorm","alchemy","black dragon"}.Any(n=>Path.GetFileName(d).IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0)))
     foreach(string dir in new[]{folder,Path.Combine(folder,"bin")}){
      if(!Directory.Exists(dir))continue;
      foreach(string file in Directory.GetFiles(dir,"*.exe")){
       string name=Path.GetFileNameWithoutExtension(file);
       if(new[]{"SecondLife","Firestorm","Alchemy","BlackDragon","Black Dragon"}.Any(n=>name.StartsWith(n,StringComparison.OrdinalIgnoreCase))&&name.IndexOf("crash",StringComparison.OrdinalIgnoreCase)<0&&!viewer.Items.Contains(file))viewer.Items.Add(file);
      }
     }
   }catch(IOException){}catch(UnauthorizedAccessException){}
  }
 }
}

