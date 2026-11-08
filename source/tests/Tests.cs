// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Linq;
using SLViewerAssociation;
class Tests {
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static void Throws(Action action,string message){try{action();}catch{checks++;return;}throw new Exception(message);}
 static int Main(string[] args){try{
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);string token=Guid.NewGuid().ToString("N");
  var store=new SettingsStore(Path.Combine(root,token+".xml"));var data=store.Load();Check(data.AskEveryTime&&data.Viewers.Count==0,"New settings must ask");
  string fake=Path.GetFullPath(args[1]);string second=Path.Combine(root,"second viewer "+token+".exe");File.Copy(fake,second);
  var first=data.Upsert("日本語 & First",fake);var other=data.Upsert("Second",second);store.Save(data);
  data=store.Load();Check(data.Viewers.Count==2&&data.Viewers[0].Name=="日本語 & First","Profile round trip");
  Throws(()=>data.Upsert("Duplicate",fake),"Duplicate executable accepted");
  LinkLaunch.Remember(data,data.Viewers[1],true);store.Save(data);data=store.Load();Check(!data.AskEveryTime&&data.AutomaticViewer().Id==other.Id,"Remembered selection not restored");
  LinkLaunch.Remember(data,data.Viewers[0],false);store.Save(data);data=store.Load();Check(data.AskEveryTime&&data.LastViewerId==first.Id&&data.AutomaticViewer()==null,"Ask mode not restored");
  data.Upsert("Renamed",fake,first.Id);store.Save(data);Check(store.Load().Viewers[0].Name=="Renamed","Rename failed");
  LinkLaunch.Remember(data,data.Viewers[1],true);data.Viewers[1].FilePath=Path.Combine(root,"missing "+token+".exe");store.Save(data);Check(store.Load().AutomaticViewer()==null,"Missing viewer did not fall back to selection");
  Check(data.Remove(other.Id)&&data.AskEveryTime&&data.LastViewerId=="","Removing selected viewer did not reset mode");store.Save(data);Check(store.Load().Viewers.Count==1,"Profile removal not saved");
  Check(data.Merge(new[]{new ViewerProfile{Name="Repeated",FilePath=fake}})==0,"Discovery merged a duplicate");
  Check(data.Merge(new[]{new ViewerProfile{Name="New",FilePath=second}})==1,"Discovery failed to add a profile");
  Check(Discovery.IsViewerFile("AlchemyBeta.exe")&&!Discovery.IsViewerFile("FirestormCrashLogger.exe")&&!Discovery.IsViewerFile("SecondLifeUpdater.exe"),"Discovery includes helper executables");
  Check(Discovery.ExecutableFromCommand("\""+fake+"\" --multiple")==fake,"Quoted executable parsing failed");
  Check(Association.HandlerCommand().EndsWith("\" --open \"%1\""),"Handler command lacks dynamic URL placeholder");
  foreach(string url in new[]{"secondlife://Davros/40/200/3000","secondlife://Other%20Region/1/2/3","secondlife:///app/agent/example/about"}){
   Check(LinkLaunch.ValidateUrl(url)==url,"Link altered");Environment.SetEnvironmentVariable("SLVIEWER_TEST_URL",url);
   using(var p=LinkLaunch.Start(new ViewerProfile{Name="Fake",FilePath=fake},url)){Check(p.WaitForExit(5000)&&p.ExitCode==0,"Viewer received incorrect arguments");}
  }
  foreach(string url in new[]{"","https://example.com/","secondlife://R/1/2/3"+(char)34+" extra","secondlife://R/1/2/3"+(char)10})Throws(()=>LinkLaunch.ValidateUrl(url),"Invalid URL accepted");
  string corrupt=Path.Combine(root,token+"-corrupt.xml");File.WriteAllText(corrupt,"<broken/>");var badStore=new SettingsStore(corrupt);Throws(()=>badStore.Load(),"Corrupt settings accepted");Check(File.ReadAllText(corrupt)=="<broken/>","Corrupt settings overwritten");
  File.WriteAllText(corrupt,"<!DOCTYPE settings [<!ENTITY sample 'x'>]><settings version='1'>&sample;</settings>");Throws(()=>badStore.Load(),"DTD accepted");
  Console.WriteLine("PASS: "+checks+" checks. No Windows associations changed; no real viewer launched.");return 0;
 }catch(Exception e){Console.WriteLine(e.GetType().FullName+": "+e.Message);return 1;}}
}
