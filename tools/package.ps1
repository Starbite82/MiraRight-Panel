param([string]$Dotnet='dotnet',[string]$Version='1.1.1',[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$stage=Join-Path $root ('work/publish-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stage|Out-Null
$app=Join-Path $stage 'app';$hostStage=Join-Path $stage 'host';$package=Join-Path $stage 'AIO-Display'
foreach($item in @(@('src/AioDisplay.App/AioDisplay.App.csproj',$app),@('src/AioDisplay.SensorHost/AioDisplay.SensorHost.csproj',$hostStage))){
 & $Dotnet publish (Join-Path $root $item[0]) -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true -o $item[1]
 if($LASTEXITCODE -ne 0){throw 'Publish failed'}
}
New-Item -ItemType Directory -Force $package|Out-Null
Copy-Item -Path (Join-Path $app '*') -Destination $package -Recurse
$sensors=Join-Path $package 'sensors'
New-Item -ItemType Directory -Force $sensors|Out-Null
Copy-Item -Path (Join-Path $hostStage '*') -Destination $sensors -Recurse
if(Test-Path -LiteralPath (Join-Path $root 'IMPRINT.txt')){Copy-Item -LiteralPath (Join-Path $root 'IMPRINT.txt') -Destination $package}
foreach($name in @('LIESMICH.txt','settings.example.ini')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $package $name)}
Copy-Item -LiteralPath (Join-Path $root 'settings.example.ini') -Destination (Join-Path $package 'settings.ini')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AIO-Display-Vorschau.cmd') -Destination $package
& (Join-Path $PSScriptRoot 'third-party-notices.ps1') -PackageDirectory $package
& (Join-Path $PSScriptRoot 'source-bundle.ps1') -PackageDirectory $package
$forbidden=@(Get-ChildItem -LiteralPath $package -Recurse -File|Where-Object {$_.Name -match '^(FanControl(\.IPC)?|MSIAfterburner|Afterburner|USBLCDNEW|TRCC)\.(dll|exe)$'})
if($forbidden.Count){throw 'Proprietary vendor binary in package'}
$hashes=foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File){
 $hash=Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256
 '{0}  {1}' -f $hash.Hash.ToLowerInvariant(),$file.FullName.Substring($package.Length+1)
}
$hashes|Set-Content -LiteralPath (Join-Path $package 'SHA256SUMS.txt') -Encoding utf8
& (Join-Path $PSScriptRoot 'check-package.ps1') -PackageDirectory $package
if(-not $OutputDirectory){$OutputDirectory=Split-Path $root}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$zip=Join-Path $OutputDirectory ('AIO-Display-v'+$Version+'-windows-x64.zip')
Compress-Archive -LiteralPath $package -DestinationPath $zip -Force
$hash=Get-FileHash -LiteralPath $zip -Algorithm SHA256
('{0}  {1}' -f $hash.Hash.ToLowerInvariant(),[IO.Path]::GetFileName($zip))|Set-Content -LiteralPath ($zip+'.sha256') -Encoding ascii
$package|Set-Content -LiteralPath (Join-Path $root 'work/last-package-path.txt')
Get-Item -LiteralPath $zip
