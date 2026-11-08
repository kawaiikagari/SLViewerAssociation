// SPDX-License-Identifier: GPL-3.0-or-later
// Distributed under GNU GPL version 3 or later, without warranty. See LICENSE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
namespace SLViewerAssociation {
 public static class Discovery {
  static readonly string[] Names={"secondlife","second life","firestorm","alchemy","blackdragon","black dragon","singularity","cool vl","cool_vl","coolvl"};
  static bool Known(string name){return Names.Any(n=>name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);}
  public static bool IsViewerFile(string file){
   string name=Path.GetFileNameWithoutExtension(file);
   return file.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)&&Names.Any(n=>name.StartsWith(n,StringComparison.OrdinalIgnoreCase))&&!new[]{"crash","update","uninst","setup","selector","chooser","association"}.Any(n=>name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
  }
  public static string ExecutableFromCommand(string raw){
   string s=Environment.ExpandEnvironmentVariables((raw??"").Trim());if(s.StartsWith("\"")){int end=s.IndexOf('"',1);return end>1?s.Substring(1,end-1):"";}
   int exe=s.IndexOf(".exe",StringComparison.OrdinalIgnoreCase);return exe<0?"":s.Substring(0,exe+4);
  }
  static bool LocalPath(string path){
   try{string p=Path.GetFullPath(path);return p.Length>2&&char.IsLetter(p[0])&&p[1]==':'&&new DriveInfo(Path.GetPathRoot(p)).DriveType!=DriveType.Network;}catch(ArgumentException){return false;}catch(IOException){return false;}
  }
  public static List<ViewerProfile> Find(){
   var found=new Dictionary<string,ViewerProfile>(StringComparer.OrdinalIgnoreCase);var folders=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   Action<string> add=file=>{try{if(!string.IsNullOrWhiteSpace(file)&&IsViewerFile(file)&&LocalPath(file)&&File.Exists(file)){string p=Path.GetFullPath(file);if(!found.ContainsKey(p))found[p]=new ViewerProfile{Name=Path.GetFileNameWithoutExtension(p),FilePath=p};}}catch(ArgumentException){}catch(NotSupportedException){}};
   Action<string> scan=dir=>{try{if(LocalPath(dir)&&Directory.Exists(dir)){foreach(string file in Directory.GetFiles(dir,"*.exe"))add(file);string bin=Path.Combine(dir,"bin");if(Directory.Exists(bin))foreach(string file in Directory.GetFiles(bin,"*.exe"))add(file);}}catch(IOException){}catch(UnauthorizedAccessException){}};
   foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs")}.Distinct())try{if(Directory.Exists(root))foreach(string dir in Directory.GetDirectories(root).Where(d=>Known(Path.GetFileName(d))))folders.Add(dir);}catch(IOException){}catch(UnauthorizedAccessException){}
   foreach(RegistryHive hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(RegistryView view in new[]{RegistryView.Registry32,RegistryView.Registry64})try{
    using(var baseKey=RegistryKey.OpenBaseKey(hive,view)){
     using(var uninstall=baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))if(uninstall!=null)foreach(string id in uninstall.GetSubKeyNames())using(var app=uninstall.OpenSubKey(id)){
      if(app==null||!Known((string)app.GetValue("DisplayName","")??""))continue;
      string dir=Environment.ExpandEnvironmentVariables((string)app.GetValue("InstallLocation","")??"").Trim('"');if(dir.Length>0)folders.Add(dir);
      string icon=ExecutableFromCommand(app.GetValue("DisplayIcon","") as string);add(icon);if(icon.Length>0)folders.Add(Path.GetDirectoryName(icon));
     }
     using(var paths=baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"))if(paths!=null)foreach(string id in paths.GetSubKeyNames().Where(Known))using(var app=paths.OpenSubKey(id))if(app!=null)add(ExecutableFromCommand(app.GetValue("") as string));
    }
   }catch(System.Security.SecurityException){}catch(UnauthorizedAccessException){}catch(IOException){}
   foreach(string folder in folders.Where(f=>!string.IsNullOrWhiteSpace(f)))scan(folder);
   foreach(var process in Process.GetProcesses())using(process)try{if(Known(process.ProcessName)){var module=process.MainModule;if(module!=null)add(module.FileName);}}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}catch(System.Security.SecurityException){}
   try{add(Association.CurrentViewer());}catch(System.Security.SecurityException){}
   return found.Values.OrderBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.FilePath,StringComparer.OrdinalIgnoreCase).ToList();
  }
 }
}
