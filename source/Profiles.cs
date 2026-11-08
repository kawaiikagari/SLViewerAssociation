// SPDX-License-Identifier: GPL-3.0-or-later
// Distributed under GNU GPL version 3 or later, without warranty. See LICENSE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
namespace SLViewerAssociation {
 public sealed class ViewerProfile {
  public string Id {get;set;}
  public string Name {get;set;}
  public string FilePath {get;set;}
  public ViewerProfile(){Id=Guid.NewGuid().ToString("N");}
 }
 public sealed class AppSettings {
  public bool AskEveryTime {get;set;}
  public string LastViewerId {get;set;}
  public List<ViewerProfile> Viewers {get;set;}
  public AppSettings(){AskEveryTime=true;LastViewerId="";Viewers=new List<ViewerProfile>();}
  public ViewerProfile AutomaticViewer(){return AskEveryTime?null:Viewers.FirstOrDefault(v=>v.Id==LastViewerId&&File.Exists(v.FilePath));}
  public ViewerProfile Upsert(string name,string file,string id=null){
   string path=Association.Validate(file);name=(name??"").Trim();
   if(name.Length==0||name.Length>120)throw new ArgumentException("登録名を1～120文字で入力してください。");
   var duplicate=Viewers.FirstOrDefault(v=>v.FilePath.Equals(path,StringComparison.OrdinalIgnoreCase)&&v.Id!=id);
   if(duplicate!=null)throw new ArgumentException("この実行ファイルは「"+duplicate.Name+"」として登録済みです。");
   var profile=id==null?null:Viewers.FirstOrDefault(v=>v.Id==id);
   if(id!=null&&profile==null)throw new InvalidOperationException("登録情報が変更されました。一覧を更新してください。");
   if(profile==null){profile=new ViewerProfile();Viewers.Add(profile);}
   profile.Name=name;profile.FilePath=path;return profile;
  }
  public bool Remove(string id){
   var p=Viewers.FirstOrDefault(v=>v.Id==id);if(p==null)return false;Viewers.Remove(p);
   if(LastViewerId==id){LastViewerId="";AskEveryTime=true;}return true;
  }
  public int Merge(IEnumerable<ViewerProfile> found){int added=0;foreach(var p in found){if(Viewers.Any(v=>v.FilePath.Equals(p.FilePath,StringComparison.OrdinalIgnoreCase)))continue;Upsert(p.Name,p.FilePath);added++;}return added;}
 }
 public sealed class SettingsStore {
  public static readonly string DefaultFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SLViewerAssociation","viewers.xml");
  public string FileName {get;private set;}
  public SettingsStore():this(DefaultFile){}
  public SettingsStore(string file){FileName=Path.GetFullPath(file);}
  public AppSettings Load(){
   if(!File.Exists(FileName))return new AppSettings();
   XElement root;using(var reader=XmlReader.Create(FileName,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))root=XDocument.Load(reader).Root;
   if(root==null||root.Name!="settings"||(string)root.Attribute("version")!="1")throw new InvalidDataException("ビューアー登録情報の形式が正しくありません。");
   var data=new AppSettings{AskEveryTime=(string)root.Attribute("mode")!="remember",LastViewerId=(string)root.Attribute("last")??""};
   foreach(var e in root.Elements("viewer"))data.Viewers.Add(new ViewerProfile{Id=(string)e.Attribute("id"),Name=(string)e.Element("name"),FilePath=(string)e.Element("path")});
   Validate(data);return data;
  }
  static void Validate(AppSettings data){
   if(data.Viewers==null||data.Viewers.Count>512)throw new InvalidDataException("ビューアーの登録数が正しくありません。");
   var ids=new HashSet<string>(StringComparer.Ordinal);var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var v in data.Viewers){Guid id;
    if(v==null||!Guid.TryParse(v.Id,out id)||!ids.Add(v.Id)||string.IsNullOrWhiteSpace(v.Name)||v.Name.Length>120||string.IsNullOrWhiteSpace(v.FilePath)||!Path.IsPathRooted(v.FilePath)||!v.FilePath.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!paths.Add(Path.GetFullPath(v.FilePath)))throw new InvalidDataException("ビューアーの登録情報が正しくありません。");
   }
   if(!data.Viewers.Any(v=>v.Id==data.LastViewerId)){data.LastViewerId="";data.AskEveryTime=true;}
  }
  public void Save(AppSettings data){
   Validate(data);var root=new XElement("settings",new XAttribute("version","1"),new XAttribute("mode",data.AskEveryTime?"ask":"remember"),new XAttribute("last",data.LastViewerId??""));
   foreach(var v in data.Viewers)root.Add(new XElement("viewer",new XAttribute("id",v.Id),new XElement("name",v.Name),new XElement("path",v.FilePath)));
   string folder=Path.GetDirectoryName(FileName);Directory.CreateDirectory(folder);string temp=Path.Combine(folder,Guid.NewGuid().ToString("N")+".tmp");
   try{new XDocument(root).Save(temp);if(File.Exists(FileName))File.Replace(temp,FileName,null);else File.Move(temp,FileName);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
