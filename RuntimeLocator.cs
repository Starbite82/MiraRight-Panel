using System;
using System.IO;
public static class RuntimeLocator {
 public static string PowerShell(){
  string configured=AppSettings.Current.PowerShellPath;
  if(!string.IsNullOrWhiteSpace(configured))return configured;
  foreach(var path in new[]{Path.Combine(Log.Root,@"runtime\pwsh.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"PowerShell\7\pwsh.exe")})if(File.Exists(path))return path;
  foreach(var folder in (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator)){try{var path=Path.Combine(folder.Trim('"'),"pwsh.exe");if(File.Exists(path))return path;}catch(ArgumentException){}}
  return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),@".cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe");
 }
}
