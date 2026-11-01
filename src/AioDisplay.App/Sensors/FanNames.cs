// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class FanNames
{
    static readonly object sync = new();
    static Dictionary<string, string> names;
    static string FilePath => Path.Combine(Log.Root, "fan-names.json");
    // LHM and FanControl expose the same Super-I/O channel with/without an instance segment.
    public static string Key(string id) => id?.StartsWith("ADLX/") == true ? "selected-gpu" : Regex.Replace(id ?? "", @"^(/lpc/[^/]+)/\d+(/fan/\d+)$", "$1$2");
    static Dictionary<string, string> Load()
    {
        if (names != null) return names;
        try { names = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { names = new(); }
        return names;
    }
    public static string Get(string id, string fallback) { lock (sync) return Load().TryGetValue(Key(id), out string name) && !string.IsNullOrWhiteSpace(name) ? name : fallback; }
    public static void Save(IEnumerable<KeyValuePair<string, string>> values)
    {
        lock (sync)
        {
            var next = new Dictionary<string, string>(Load());
            foreach (var pair in values) { string key = Key(pair.Key), value = (pair.Value ?? "").Trim(); if (value.Length > 60 || value.Any(char.IsControl)) throw new InvalidDataException("Lüfternamen dürfen höchstens 60 Zeichen ohne Steuerzeichen enthalten."); if (value == "") next.Remove(key); else next[key] = value; }
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(next, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, FilePath, true); names = next;
        }
    }
    public static void Import(IEnumerable<FanReading> fans)
    {
        lock (sync) Save(fans.Where(f => !Load().ContainsKey(Key(f.Id))).Select(f => new KeyValuePair<string, string>(f.Id, FanData.ShortName(f))).ToArray());
    }
}
