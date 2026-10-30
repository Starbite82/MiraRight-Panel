param([string]$Version='1.0.0',[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(-not $OutputDirectory){$OutputDirectory=Split-Path $root}
$stage=Join-Path $root ('work/package-'+[Guid]::NewGuid().ToString('N'))
$package=Join-Path $stage 'AIO-Display';New-Item -ItemType Directory -Path $package -Force|Out-Null
$files=@('BLUB-Display.exe','LibreHardwareBridge.ps1','FanControlBridge.ps1','SnapshotWriter.ps1','SnapshotFiles.cs','DependencyPaths.ps1','settings.example.ini','LIESMICH.txt')
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $root $file) -Destination (Join-Path $package $file)}
# A fresh install uses the tested profile; existing users keep their settings.ini.
Copy-Item -LiteralPath (Join-Path $root 'settings.example.ini') -Destination (Join-Path $package 'settings.ini')
$hashes=foreach($file in Get-ChildItem -LiteralPath $package -File){$hash=Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256;'{0}  {1}' -f $hash.Hash.ToLowerInvariant(),$file.Name}
$hashes|Set-Content -LiteralPath (Join-Path $package 'SHA256SUMS.txt') -Encoding utf8
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$zip=Join-Path $OutputDirectory ('AIO-Display-v'+$Version+'-windows-x64.zip')
Compress-Archive -LiteralPath $package -DestinationPath $zip -Force
$hash=Get-FileHash -LiteralPath $zip -Algorithm SHA256
('{0}  {1}' -f $hash.Hash.ToLowerInvariant(),[IO.Path]::GetFileName($zip))|Set-Content -LiteralPath ($zip+'.sha256') -Encoding ascii
Get-Item -LiteralPath $zip;Get-Content -LiteralPath ($zip+'.sha256')
