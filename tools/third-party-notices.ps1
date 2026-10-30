param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot
$cache=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget/packages'}
$notice=Join-Path $PackageDirectory 'licenses';New-Item -ItemType Directory -Force $notice|Out-Null
$entries=[Collections.Generic.List[string]]::new();$entries.Add('Third-party components; original package metadata and license texts follow. Fan Control binaries are not redistributed.')
$lock=Get-Content -LiteralPath (Join-Path $root 'src/AioDisplay.SensorHost/packages.lock.json') -Raw|ConvertFrom-Json
$seen=@{}
foreach($framework in $lock.dependencies.PSObject.Properties){foreach($package in $framework.Value.PSObject.Properties){
 if($package.Value.type -eq 'Project'){continue};$id=$package.Name;$version=$package.Value.resolved;$key=$id+'/'+$version;if($seen[$key]){continue};$seen[$key]=$true
 $folder=Join-Path $cache $key.ToLowerInvariant();$spec=Get-ChildItem -LiteralPath $folder -Filter '*.nuspec'|Select-Object -First 1
 if(-not $spec){throw ('Package metadata missing: '+$key)}
 [xml]$xml=Get-Content -LiteralPath $spec.FullName -Raw;$metadata=$xml.package.metadata
 $target=Join-Path $notice ($id+'-'+$version);New-Item -ItemType Directory -Force $target|Out-Null;Copy-Item -LiteralPath $spec.FullName -Destination $target
 $entries.Add($key+' | license: '+$metadata.license.InnerText+' | '+$metadata.projectUrl)
 foreach($file in Get-ChildItem -LiteralPath $folder -File|Where-Object {$_.Name -match '^(LICENSE|LICENCE|COPYING|NOTICE|AUTHORS)'}){Copy-Item -LiteralPath $file.FullName -Destination $target}
 if($metadata.license.type -eq 'file'){$path=Join-Path $folder $metadata.license.InnerText;if(Test-Path -LiteralPath $path){Copy-Item -LiteralPath $path -Destination $target}}
}}
$entries.Add('LibreHardwareMonitor source corresponding to NuGet 0.9.6: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/3d331e3370efb858411f19511373eff65a218701')
$entries.Add('Library source is unmodified. .NET runtime notices are included by dotnet publish.')
$entries|Set-Content -LiteralPath (Join-Path $PackageDirectory 'THIRD-PARTY-COMPONENTS.txt') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $root 'docs/licenses/MPL-2.0.txt') -Destination $notice
