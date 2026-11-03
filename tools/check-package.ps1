param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
$required=@(
 'FlexPanel.Core.dll','sensors/FlexPanel.Core.dll','sensors/tools/PresentMon.exe','licenses/FlexPanel.Core-MIT.txt','displays.example.json',
 'LICENSE','LICENSE-MAP.txt','project-licenses/GPL-3.0-only.txt','project-licenses/MIT.txt','sensors/LICENSE','licenses/AioDisplay.Contracts-MIT.txt',
 'THIRD-PARTY-COMPONENTS.txt','EXTERNAL-INTEGRATIONS.txt','CODE-PROVENANCE.txt','LICENSE-PROFILE.txt',
 'licenses/MPL-2.0.txt','licenses/LibreHardwareMonitorLib/LICENSE',
 'licenses/LibreHardwareMonitorLib/THIRD-PARTY-NOTICES.txt',
 'licenses/LibreHardwareMonitorLib/LibreHardwareMonitorLib/Resources/PawnIo/COPYING',
 'licenses/DiskInfoToolkit/THIRD-PARTY-NOTICES','licenses/PawnIO.Modules/COPYING',
 'licenses/HidSharp-2.6.4/LICENSE.txt','licenses/Mono.Posix.NETStandard-1.0.0/LICENSE.txt',
 'licenses/microsoft.netcore.app.runtime.win-x64-10.0.13/LICENSE.TXT',
 'licenses/microsoft.netcore.app.runtime.win-x64-10.0.13/THIRD-PARTY-NOTICES.TXT',
 'licenses/microsoft.windowsdesktop.app.runtime.win-x64-10.0.13/LICENSE',
 'sources/SOURCE-MANIFEST.json','sources/README-SOURCES.txt','sources/FlexPanel-source.zip'
)
foreach($relative in $required){if(-not (Test-Path -LiteralPath (Join-Path $PackageDirectory $relative))){throw ('Missing legal/package file: '+$relative)}}
$records=@(Get-Content -LiteralPath (Join-Path $PackageDirectory 'sources/SOURCE-MANIFEST.json') -Raw|ConvertFrom-Json)
$components=@('FlexPanel','LibreHardwareMonitorLib','BlackSharp.Core','DiskInfoToolkit','RAMSPDToolkit-NDD','PawnIO.Modules')
if($records.Count -ne $components.Count){throw 'Incomplete corresponding source manifest'}
foreach($name in $components){if(@($records|Where-Object {$_.Component -eq $name}).Count -ne 1){throw ('Missing or duplicated source: '+$name)}}
foreach($record in $records){
 $path=Join-Path (Join-Path $PackageDirectory 'sources') $record.IncludedSourceArchive
 if((Get-FileHash -LiteralPath $path).Hash -ne $record.IncludedSha256){throw ('Source checksum mismatch: '+$record.Component)}
}
$files=@(Get-ChildItem -LiteralPath $PackageDirectory -Recurse -File)
if(@($files|Where-Object {$_.Name -match '^(FanControl(\.IPC)?|MSIAfterburner|Afterburner|TRCC|USBLCDNEW)\.(dll|exe)$'}).Count){throw 'Vendor binary redistributed unexpectedly'}
$entries=0
foreach($line in Get-Content -LiteralPath (Join-Path $PackageDirectory 'SHA256SUMS.txt')){
 if($line -notmatch '^([a-f0-9]{64})  (.+)$'){throw 'Malformed manifest'}
 $hash=$Matches[1];$path=Join-Path $PackageDirectory $Matches[2]
 if((Get-FileHash -LiteralPath $path).Hash -ne $hash){throw ('File checksum mismatch: '+$path)}
 $entries++
}
$profile=Get-Content -LiteralPath (Join-Path $PackageDirectory 'LICENSE-PROFILE.txt') -Raw
$licensePending=$profile -match 'DECISION PENDING'
if($licensePending -or $profile -notmatch 'GPL-3.0-only' -or $profile -notmatch 'MIT'){throw 'Project license grant/scope missing'}
$gpl=Get-Content -LiteralPath (Join-Path $PackageDirectory 'project-licenses/GPL-3.0-only.txt') -Raw
if($gpl -notmatch 'GNU GENERAL PUBLIC LICENSE' -or $gpl -notmatch 'Version 3'){throw 'GPL text missing'}
$mit=Get-Content -LiteralPath (Join-Path $PackageDirectory 'project-licenses/MIT.txt') -Raw
if($mit -notmatch 'Permission is hereby granted' -or $mit -notmatch 'AIO Display contributors'){throw 'MIT grant missing'}
if((Get-FileHash -LiteralPath (Join-Path $PackageDirectory 'sensors/LICENSE')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $PackageDirectory 'project-licenses/MIT.txt')).Hash){throw 'Sensor license differs'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$ownZip=[IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory 'sources/FlexPanel-source.zip'))
try{
 foreach($path in @('FlexPanel/LICENSE','FlexPanel/LICENSE-MAP.txt','FlexPanel/project-licenses/MIT.txt','FlexPanel/src/AioDisplay.SensorHost/LICENSE','FlexPanel/src/AioDisplay.Contracts/LICENSE')){
  if(-not $ownZip.GetEntry($path)){throw ('Source license missing: '+$path)}
 }
}finally{$ownZip.Dispose()}
$hasImprint=Test-Path -LiteralPath (Join-Path $PackageDirectory 'IMPRINT.txt')
if($hasImprint){
 $provider=Get-Content -LiteralPath (Join-Path $PackageDirectory 'IMPRINT.txt') -Raw
 if([string]::IsNullOrWhiteSpace($provider) -or $provider.Contains('VORLAGE —') -or $provider.Contains('[Vollständiger Vor- und Nachname]')){throw 'Unfinished imprint template in package'}
}
[ordered]@{PackageFilesVerified=$entries;SourceArchivesVerified=$records.Count;RequiredNoticeFilesPresent=$required.Count;VendorBinariesIncluded=$false;ProjectLicenseDecisionPending=$licensePending;ImprintDeferred=(-not $hasImprint);ImprintFileIncluded=$hasImprint}|ConvertTo-Json
