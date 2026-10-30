param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
$required=@(
 'THIRD-PARTY-COMPONENTS.txt','EXTERNAL-INTEGRATIONS.txt','CODE-PROVENANCE.txt','LICENSE-PROFILE.txt',
 'licenses/MPL-2.0.txt','licenses/LibreHardwareMonitorLib/LICENSE',
 'licenses/LibreHardwareMonitorLib/THIRD-PARTY-NOTICES.txt',
 'licenses/LibreHardwareMonitorLib/LibreHardwareMonitorLib/Resources/PawnIo/COPYING',
 'licenses/DiskInfoToolkit/THIRD-PARTY-NOTICES','licenses/PawnIO.Modules/COPYING',
 'licenses/HidSharp-2.6.4/LICENSE.txt','licenses/Mono.Posix.NETStandard-1.0.0/LICENSE.txt',
 'licenses/microsoft.netcore.app.runtime.win-x64-10.0.13/LICENSE.TXT',
 'licenses/microsoft.netcore.app.runtime.win-x64-10.0.13/THIRD-PARTY-NOTICES.TXT',
 'licenses/microsoft.windowsdesktop.app.runtime.win-x64-10.0.13/LICENSE',
 'sources/SOURCE-MANIFEST.json','sources/README-SOURCES.txt','sources/AIO-Display-source.zip'
)
foreach($relative in $required){if(-not (Test-Path -LiteralPath (Join-Path $PackageDirectory $relative))){throw ('Missing legal/package file: '+$relative)}}
foreach($record in @(Get-Content -LiteralPath (Join-Path $PackageDirectory 'sources/SOURCE-MANIFEST.json') -Raw|ConvertFrom-Json)){
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
$licensePending=(Get-Content -LiteralPath (Join-Path $PackageDirectory 'LICENSE-PROFILE.txt') -Raw) -match 'DECISION PENDING'
[ordered]@{PackageFilesVerified=$entries;SourceArchivesVerified=6;RequiredNoticeFilesPresent=$required.Count;VendorBinariesIncluded=$false;ProjectLicenseDecisionPending=$licensePending;ImprintDeferred=$true}|ConvertTo-Json
