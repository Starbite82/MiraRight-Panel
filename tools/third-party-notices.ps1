param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$cache=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget/packages'}
$notice=Join-Path $PackageDirectory 'licenses'
New-Item -ItemType Directory -Force $notice|Out-Null
Copy-Item (Join-Path $root 'src/FlexPanel.Core/LICENSE') (Join-Path $notice 'FlexPanel.Core-MIT.txt')
Copy-Item -Path (Join-Path $root 'docs/licenses/flexpanel/*') -Destination $notice
$entries=[Collections.Generic.List[string]]::new()
$entries.Add('Third-party components. Fan Control binaries are not redistributed.')
$entries.Add('Metadata can include platform variants that are not shipped in this Windows package.')
$lock=Get-Content -LiteralPath (Join-Path $root 'src/AioDisplay.SensorHost/packages.lock.json') -Raw|ConvertFrom-Json
$seen=@{}
foreach($framework in $lock.dependencies.PSObject.Properties){
 foreach($package in $framework.Value.PSObject.Properties){
  if($package.Value.type -eq 'Project'){continue}
  $id=$package.Name;$version=$package.Value.resolved;$key=$id+'/'+$version
  if($seen[$key]){continue};$seen[$key]=$true
  $folder=Join-Path $cache $key.ToLowerInvariant()
  $spec=Get-ChildItem -LiteralPath $folder -Filter '*.nuspec'|Select-Object -First 1
  if(-not $spec){throw ('Package metadata missing: '+$key)}
  [xml]$xml=Get-Content -LiteralPath $spec.FullName -Raw
  $metadata=$xml.package.metadata
  $target=Join-Path $notice ($id+'-'+$version)
  New-Item -ItemType Directory -Force $target|Out-Null
  Copy-Item -LiteralPath $spec.FullName -Destination $target
  $license=if($metadata.license){$metadata.license.InnerText}else{$metadata.licenseUrl}
  $entries.Add($key+' | license: '+$license+' | '+$metadata.projectUrl)
  if($metadata.copyright){$entries.Add('Copyright: '+$metadata.copyright)}
  foreach($file in Get-ChildItem -LiteralPath $folder -File|Where-Object {$_.Name -match '^(LICENSE|LICENCE|COPYING|NOTICE|AUTHORS|THIRD-PARTY)'}){
   Copy-Item -LiteralPath $file.FullName -Destination $target
  }
  if($metadata.license.type -eq 'file'){
   $path=Join-Path $folder $metadata.license.InnerText
   if(Test-Path -LiteralPath $path){Copy-Item -LiteralPath $path -Destination $target}
  }
  if($license -eq 'MPL-2.0'){
   $repository=[string]$metadata.repository.url;$commit=[string]$metadata.repository.commit
   if(-not $repository -or -not $commit){throw ('Missing MPL source version: '+$key)}
   $source=$repository.TrimEnd('/')+'/tree/'+$commit
   $entries.Add('MPL source for '+$key+': '+$source)
   @('The library is unmodified. Its corresponding source is available under MPL-2.0 at:',$source,'License text: ../MPL-2.0.txt')|Set-Content -LiteralPath (Join-Path $target 'SOURCE.txt') -Encoding utf8
  }
  if($id -eq 'Mono.Posix.NETStandard'){
   Copy-Item -LiteralPath (Join-Path $root 'docs/licenses/Mono-LICENSE.txt') -Destination (Join-Path $target 'LICENSE.txt')
   $entries.Add('Mono upstream license supplied by the package license URL: https://github.com/mono/mono/blob/main/LICENSE')
  }
 }
}
[xml]$properties=Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw
$runtimeVersion=[string]$properties.Project.PropertyGroup.RuntimeFrameworkVersion
foreach($runtime in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')){
 $folder=Join-Path $cache ($runtime+'/'+$runtimeVersion)
 $target=Join-Path $notice ($runtime+'-'+$runtimeVersion)
 New-Item -ItemType Directory -Force $target|Out-Null
 $files=@(Get-ChildItem -LiteralPath $folder -File|Where-Object {$_.Name -match '^(LICENSE|THIRD-PARTY|NOTICE)'})
 if(-not @($files|Where-Object {$_.Name -match '^LICENSE'}).Count){throw ('Runtime license missing: '+$runtime)}
 foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination $target}
 $entries.Add($runtime+'/'+$runtimeVersion+' | runtime license and notices: licenses/'+$runtime+'-'+$runtimeVersion)
}
Copy-Item -LiteralPath (Join-Path $root 'docs/licenses/MPL-2.0.txt') -Destination $notice
$sourceManifest=Get-Content -LiteralPath (Join-Path $root 'docs/licenses/source-manifest.json') -Raw|ConvertFrom-Json
foreach($component in $sourceManifest.components){
 $target=Join-Path $notice $component.Name
 New-Item -ItemType Directory -Force $target|Out-Null
 foreach($relative in $component.NoticeFiles){
  $file=Join-Path $root ('docs/licenses/upstream/'+$component.Name+'/'+$relative)
  $dest=Join-Path $target $relative
  New-Item -ItemType Directory -Force (Split-Path $dest)|Out-Null
  Copy-Item -LiteralPath $file -Destination $dest
 }
}
$entries.Add('PawnIO.Modules 0.1.6 embedded in LHM: LGPL-2.1; matching source is included in sources/.')
$entries.Add('Additional LHM notices, including PawnIO.Modules and BSD notices: licenses/LibreHardwareMonitorLib/THIRD-PARTY-NOTICES.txt')
$entries.Add('DiskInfoToolkit CrystalDiskInfo MIT notice: licenses/DiskInfoToolkit/THIRD-PARTY-NOTICES')
$entries.Add('Corresponding MPL/LGPL sources, versions and checksums: sources/SOURCE-MANIFEST.json')
Copy-Item -LiteralPath (Join-Path $root 'docs/EXTERNAL-INTEGRATIONS.txt') -Destination $PackageDirectory
Copy-Item -LiteralPath (Join-Path $root 'docs/CODE-PROVENANCE.txt') -Destination $PackageDirectory
Copy-Item -LiteralPath (Join-Path $root 'docs/PROJECT-LICENSE-STATUS.txt') -Destination (Join-Path $PackageDirectory 'LICENSE-PROFILE.txt')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $PackageDirectory 'LICENSE')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE-MAP.txt') -Destination $PackageDirectory
Copy-Item -LiteralPath (Join-Path $root 'project-licenses') -Destination $PackageDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $root 'src/AioDisplay.SensorHost/LICENSE') -Destination (Join-Path $PackageDirectory 'sensors/LICENSE')
Copy-Item -LiteralPath (Join-Path $root 'src/AioDisplay.Contracts/LICENSE') -Destination (Join-Path $notice 'AioDisplay.Contracts-MIT.txt')
$entries.Add('Own app: GPL-3.0-only; separate sensor host and Contracts: MIT. Full texts and scope: project-licenses/ and LICENSE-MAP.txt.')
$entries|Set-Content -LiteralPath (Join-Path $PackageDirectory 'THIRD-PARTY-COMPONENTS.txt') -Encoding utf8
