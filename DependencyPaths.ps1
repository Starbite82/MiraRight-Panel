# Explicit settings take precedence over automatic discovery.
function Read-DisplaySettings {
 $settings=@{}; $path=Join-Path $PSScriptRoot 'settings.ini'
 if(Test-Path -LiteralPath $path){foreach($line in [SnapshotFiles]::ReadLines($path)){$parts=$line -split '=',2;if($parts.Count -eq 2){$settings[$parts[0]]=$parts[1]}}}
 return $settings
}
$displaySettings=Read-DisplaySettings
function Find-FanControlRoot {
 if($displaySettings['FanControlPath']){return $displaySettings['FanControlPath']}
 $candidates=[Collections.Generic.List[string]]::new()
 foreach($p in @(Get-Process -Name FanControl -ErrorAction SilentlyContinue)){try{if($p.Path){$candidates.Add((Split-Path $p.Path))}}catch{}}
 $candidates.Add((Join-Path $PSScriptRoot 'FanControl'))
 foreach($base in @($env:ProgramFiles,${env:ProgramFiles(x86)},$env:LOCALAPPDATA)){if($base){$candidates.Add((Join-Path $base 'FanControl'))}}
 foreach($candidate in $candidates){if(Test-Path -LiteralPath (Join-Path $candidate 'FanControl.IPC.dll')){return $candidate}}
 throw 'Fan Control nicht gefunden. Ordner unter Hardware & Pfade auswaehlen.'
}
function Find-LibreHardwareRoot {
 if($displaySettings['LibreHardwarePath']){return $displaySettings['LibreHardwarePath']}
 $candidates=[Collections.Generic.List[string]]::new();$candidates.Add((Join-Path $PSScriptRoot 'LibreHardwareMonitor'))
 foreach($p in @(Get-Process -Name LibreHardwareMonitor -ErrorAction SilentlyContinue)){try{if($p.Path){$candidates.Add((Split-Path $p.Path))}}catch{}}
 try{$candidates.Add((Find-FanControlRoot))}catch{}
 foreach($base in @($env:ProgramFiles,${env:ProgramFiles(x86)})){if($base){$candidates.Add((Join-Path $base 'LibreHardwareMonitor'))}}
 foreach($candidate in $candidates){if(Test-Path -LiteralPath (Join-Path $candidate 'LibreHardwareMonitorLib.dll')){return $candidate}}
 throw 'LibreHardwareMonitorLib.dll nicht gefunden. Bibliotheksordner unter Hardware & Pfade auswaehlen.'
}
