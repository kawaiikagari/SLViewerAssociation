// SPDX-License-Identifier: GPL-3.0-or-later
// Distributed under GNU GPL version 3 or later, without warranty. See LICENSE.
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
namespace SLViewerAssociation {
 public static class LinkLaunch {
  public static string ValidateUrl(string url){
   string raw=url??"";string s=raw.Trim();
   if(s.Length<=13||s.Length>8192||raw.Any(c=>char.IsControl(c)||c=='"'||c=='\\')||!s.StartsWith("secondlife://",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Second Lifeのリンクが正しくありません。");
   return s;
  }
  public static string Quote(string s){
   var b=new StringBuilder("\"");int n=0;foreach(char c in s){if(c=='\\'){n++;continue;}if(c=='"'){b.Append('\\',n*2+1).Append(c);n=0;continue;}b.Append('\\',n).Append(c);n=0;}return b.Append('\\',n*2).Append('"').ToString();
  }
  public static Process Start(ViewerProfile viewer,string url){
   if(viewer==null)throw new ArgumentException("ビューアーを選択してください。");string file=Association.Validate(viewer.FilePath);
   return Process.Start(new ProcessStartInfo(file,"--url "+Quote(ValidateUrl(url))){UseShellExecute=false,WorkingDirectory=System.IO.Path.GetDirectoryName(file)});
  }
  public static void Remember(AppSettings settings,ViewerProfile viewer,bool remember){
   if(viewer==null||!settings.Viewers.Any(v=>v.Id==viewer.Id))throw new ArgumentException("登録済みのビューアーを選択してください。");
   settings.LastViewerId=viewer.Id;settings.AskEveryTime=!remember;
  }
 }
}
