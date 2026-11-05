// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
static class DebugLogChecks {
 public static void Run(){
  string previous=Log.Root;string root=Path.Combine(previous,"debug-log");Directory.CreateDirectory(root);
  try{
   Log.Root=root;
   Parallel.For(0,500,i=>Log.Debug("test.concurrent",new { Index=i, Text=new string('x',9000) }));
   var files=Directory.GetFiles(root,"debug.log*");
   if(files.Length<2||files.Length>4)throw new Exception("Debug log rotation missing or unbounded");
   foreach(string file in files){if(new FileInfo(file).Length>1100000)throw new Exception("Debug log too large");foreach(string line in File.ReadLines(file)){using var json=JsonDocument.Parse(line);if(json.RootElement.GetProperty("Event").GetString()!="test.concurrent")throw new Exception("Interleaved log record");}}
   string blocked=Path.Combine(root,"not-a-directory");File.WriteAllText(blocked,"test");Log.Root=blocked;Log.Debug("io-failure-must-not-crash");
  }finally{Log.Root=previous;}
  Console.WriteLine("PASS: concurrent JSON debug records, bounded rotation and unavailable log directory");
 }
}
