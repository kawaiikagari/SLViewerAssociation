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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Win32;
namespace SLViewerAssociation {
 public static class Association {
  public const string ProgId="SLViewerAssociation.SecondLife";
  const string Protocol=@"Software\Classes\secondlife";
  const string App=@"Software\SLViewerAssociation";
  const string Marker="SLViewerAssociation.Command";
  static readonly string[] Paths={Protocol,@"Software\Classes\"+ProgId,App};
  static readonly string BackupFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SLViewerAssociation");
  static readonly string BackupFile=Path.Combine(BackupFolder,"before.xml");
  [DllImport("shell32.dll")]static extern void SHChangeNotify(uint id,uint flags,IntPtr a,IntPtr b);
  static void Notify(){SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
  public static string Command(string file){return "\""+Validate(file)+"\" --url \"%1\"";}
  public static string Validate(string file){
   string p=Path.GetFullPath((file??"").Trim().Trim('"'));
   if(!p.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(p))throw new ArgumentException("ビューアーの実行ファイル（.exe）を選択してください。");
   if(p.Equals(typeof(Association).Assembly.Location,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("この設定ツール自身は指定できません。");
   return p;
  }
  static XElement Capture(RegistryKey k){
   XElement e=new XElement("key");if(k==null){e.SetAttributeValue("absent",true);return e;}
   foreach(string n in k.GetValueNames()){
    var kind=k.GetValueKind(n);object data=k.GetValue(n,null,RegistryValueOptions.DoNotExpandEnvironmentNames);
    var v=new XElement("value",new XAttribute("name",n),new XAttribute("kind",kind));
    if(data is byte[])v.Value=Convert.ToBase64String((byte[])data);
    else if(data is string[])foreach(string s in (string[])data)v.Add(new XElement("item",s));
    else v.Value=Convert.ToString(data,CultureInfo.InvariantCulture);e.Add(v);
   }
   foreach(string n in k.GetSubKeyNames())using(var sub=k.OpenSubKey(n)){var e2=Capture(sub);e2.SetAttributeValue("name",n);e.Add(e2);}return e;
  }
  static void Write(RegistryKey k,XElement e){
   foreach(var v in e.Elements("value")){
    var kind=(RegistryValueKind)Enum.Parse(typeof(RegistryValueKind),(string)v.Attribute("kind"));object data=v.Value;
    if(kind==RegistryValueKind.Binary||kind==RegistryValueKind.None)data=Convert.FromBase64String(v.Value);
    if(kind==RegistryValueKind.MultiString)data=v.Elements("item").Select(x=>x.Value).ToArray();
    if(kind==RegistryValueKind.DWord)data=int.Parse(v.Value,CultureInfo.InvariantCulture);
    if(kind==RegistryValueKind.QWord)data=long.Parse(v.Value,CultureInfo.InvariantCulture);
    k.SetValue((string)v.Attribute("name"),data,kind);
   }
   foreach(var child in e.Elements("key"))using(var sub=k.CreateSubKey((string)child.Attribute("name")))Write(sub,child);
  }
  static XElement Snapshot(){
   var root=new XElement("backup");foreach(string path in Paths)using(var k=Registry.CurrentUser.OpenSubKey(path)){var e=Capture(k);e.SetAttributeValue("path",path);root.Add(e);}
   using(var registered=Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications")){
    object old=registered==null?null:registered.GetValue("SLViewerAssociation",null,RegistryValueOptions.DoNotExpandEnvironmentNames);
    var app=new XElement("registered");if(old==null)app.SetAttributeValue("absent",true);else app.Value=(string)old;root.Add(app);
   }return root;
  }
  static void RestoreSnapshot(XElement root){
   foreach(var e in root.Elements("key")){
    string path=(string)e.Attribute("path");
    if(!Paths.Contains(path))throw new InvalidDataException("バックアップのキーが正しくありません。");
   }
   if(root.Elements("key").Select(x=>(string)x.Attribute("path")).Distinct().Count()!=Paths.Length||root.Element("registered")==null)throw new InvalidDataException("バックアップの形式が正しくありません。");
   foreach(var e in root.Elements("key")){
    string path=(string)e.Attribute("path");Registry.CurrentUser.DeleteSubKeyTree(path,false);
    if(e.Attribute("absent")==null)using(var k=Registry.CurrentUser.CreateSubKey(path))Write(k,e);
   }
   var app=root.Element("registered");using(var k=Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications")){
    if(app.Attribute("absent")!=null)k.DeleteValue("SLViewerAssociation",false);else k.SetValue("SLViewerAssociation",app.Value);
   }
  }
  static bool Owned(){
   using(var k=Registry.CurrentUser.OpenSubKey(Protocol)){
    if(k==null)return false;string expected=k.GetValue(Marker) as string;
    using(var cmd=k.OpenSubKey(@"shell\open\command"))return expected!=null&&cmd!=null&&expected==(string)cmd.GetValue("");
   }
  }
  static void SaveBackup(XElement snapshot){
   Directory.CreateDirectory(BackupFolder);string temp=Path.Combine(BackupFolder,Guid.NewGuid().ToString("N")+".tmp");
   try{new XDocument(snapshot).Save(temp);if(File.Exists(BackupFile))File.Replace(temp,BackupFile,null);else File.Move(temp,BackupFile);}
   finally{if(File.Exists(temp))File.Delete(temp);}
  }
  static void SetProtocol(string path,string command,string viewer){
   using(var k=Registry.CurrentUser.CreateSubKey(path)){
    k.SetValue("","URL:Second Life");k.SetValue("URL Protocol","");k.SetValue(Marker,command);
    using(var icon=k.CreateSubKey("DefaultIcon"))icon.SetValue("","\""+viewer+"\",0");
    using(var shell=k.CreateSubKey("shell"))shell.SetValue("","open");
    using(var open=k.CreateSubKey(@"shell\open")){
     open.DeleteValue("DelegateExecute",false);using(var cmd=open.CreateSubKey("command")){cmd.DeleteValue("DelegateExecute",false);cmd.SetValue("",command);}
    }k.DeleteSubKeyTree(@"shell\open\ddeexec",false);
   }
  }
  public static void Apply(string viewer){
   string file=Validate(viewer),command=Command(file);var before=Snapshot();
   if(Owned()){if(!File.Exists(BackupFile))throw new InvalidOperationException("復元用バックアップが見つかりません。");}else SaveBackup(before);
   try{
    SetProtocol(Protocol,command,file);SetProtocol(@"Software\Classes\"+ProgId,command,file);
    using(var k=Registry.CurrentUser.CreateSubKey(App+@"\Capabilities")){
     k.SetValue("ApplicationName","Second Life ビューアー（選択済み）");k.SetValue("ApplicationDescription","設定したビューアーでSecond Lifeのリンクを開きます。");
     using(var urls=k.CreateSubKey("URLAssociations"))urls.SetValue("secondlife",ProgId);
    }
    using(var k=Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))k.SetValue("SLViewerAssociation",App+@"\Capabilities");
   }catch{RestoreSnapshot(before);throw;}Notify();
  }
  public static void Restore(){
   if(!File.Exists(BackupFile))throw new InvalidOperationException("このツールで保存したバックアップがありません。");
   if(!Owned())throw new InvalidOperationException("関連付けが他のアプリによって変更されています。上書きを避けるため復元を中止しました。");
   var old=XDocument.Load(BackupFile).Root;var before=Snapshot();
   try{RestoreSnapshot(old);}catch{RestoreSnapshot(before);throw;}File.Delete(BackupFile);Notify();
  }
  public static string EffectiveCommand(){
   string prog="secondlife";
   using(var choice=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\secondlife\UserChoice"))if(choice!=null)prog=choice.GetValue("ProgId","secondlife") as string;
   using(var k=Registry.ClassesRoot.OpenSubKey(prog+@"\shell\open\command"))return k==null?"":k.GetValue("") as string??"";
  }
  public static string CurrentViewer(){
   string cmd=EffectiveCommand().Trim();if(cmd.StartsWith("\"")){int end=cmd.IndexOf('"',1);if(end>1)return cmd.Substring(1,end-1);}
   int exe=cmd.IndexOf(".exe",StringComparison.OrdinalIgnoreCase);return exe>=0?cmd.Substring(0,exe+4):cmd;
  }
  public static void OpenDefaults(){Process.Start("ms-settings:defaultapps");}
 }
}


