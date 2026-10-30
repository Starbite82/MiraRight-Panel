using System;
using System.IO;
using System.Threading.Tasks;
static class SnapshotFilesCheck
{
    static void Main()
    {
        string path = Path.Combine(Log.Root, "snapshot-sharing-test.txt");
        try
        {
            File.WriteAllText(path, "old");
            using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) if (SnapshotFiles.Publish(path, new[] { "blocked" })) throw new Exception("Expected blocked replacement");
            if (!SnapshotFiles.Publish(path, new[] { "seq=0", "Grüße" })) throw new Exception("Recovery after blocked reader");
            using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) if (!SnapshotFiles.Publish(path, new[] { "seq=1", "Grüße" })) throw new Exception("Shared-reader replacement");
            var writer = Task.Run(() => { for (int i = 2; i <= 300; i++) if (!SnapshotFiles.Publish(path, new[] { "seq=" + i, "Grüße" })) throw new Exception("Concurrent publication: " + SnapshotFiles.LastWriteError); });
            for (int i = 0; i < 500; i++) { var lines = SnapshotFiles.ReadLines(path); if (lines.Length != 2 || !lines[0].StartsWith("seq=") || lines[1] != "Grüße") throw new Exception("Incomplete or incorrectly decoded snapshot"); }
            writer.GetAwaiter().GetResult(); if (SnapshotFiles.ReadLines(path)[0] != "seq=300") throw new Exception("Final snapshot lost");
            Console.WriteLine("PASS: locked-reader recovery, shared-delete replacement, concurrent complete snapshots and UTF-8");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
