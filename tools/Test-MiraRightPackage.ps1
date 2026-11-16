# SPDX-License-Identifier: GPL-3.0-only
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PackageDirectory).Path.TrimEnd('\','/')
$manifest = 'SHA256SUMS.txt'
$files = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($line in Get-Content -LiteralPath (Join-Path $root $manifest)) {
    if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Ungültiges Paketmanifest.' }
    $hash = $Matches[1]
    $relative = $Matches[2].Replace('/','\')
    if ($relative -match '(^|\\)\.{1,2}(\\|$)|[:*?"<>|]' -or $relative.StartsWith('\') -or $relative -ieq $manifest) {
        throw "Ungültiger Pfad im Paketmanifest: $relative"
    }
    $files.Add($relative, $hash)
}
foreach ($required in @(
    'MiraRightPanel.exe', 'MiraRightPanel.dll', 'MiraRightPanel.deps.json', 'MiraRightPanel.runtimeconfig.json',
    'sensors\MiraRightPanel.SensorHost.exe', 'sensors\MiraRightPanel.SensorHost.dll',
    'sensors\MiraRightPanel.SensorHost.deps.json', 'sensors\MiraRightPanel.SensorHost.runtimeconfig.json',
    'sensors\LibreHardwareMonitorLib.dll', 'sensors\tools\PresentMon.exe'
)) {
    if (!$files.ContainsKey($required)) { throw "Benötigte Programmdatei fehlt im Paket: $required" }
}
foreach ($shared in 'AioDisplay.Contracts.dll','FlexPanel.Core.dll') {
    if (!$files.ContainsKey($shared) -or !$files.ContainsKey("sensors\$shared") -or $files[$shared] -ne $files["sensors\$shared"]) {
        throw "Panel und Sensorprozess enthalten unterschiedliche Bibliotheken: $shared"
    }
}
$verified = 0
foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse -Force) {
    if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Unerwartete Verknüpfung: $($file.FullName)" }
    $relative = $file.FullName.Substring($root.Length + 1)
    if ($relative -ieq $manifest) { continue }
    if (!$files.ContainsKey($relative)) { throw "Datei fehlt im Paketmanifest: $relative" }
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne $files[$relative]) { throw "Prüfsumme stimmt nicht: $relative" }
    $verified++
}
if ($verified -ne $files.Count) { throw 'Im Paket fehlen Dateien aus dem Manifest.' }

$app = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $root 'MiraRightPanel.dll')).ProductVersion
$hostVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $root 'sensors\MiraRightPanel.SensorHost.dll')).ProductVersion
if (!$app -or $app -cne $hostVersion) { throw "Panel und Sensorprozess stammen aus unterschiedlichen Builds: $app / $hostVersion" }
$verified
