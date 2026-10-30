param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root=Split-Path $PSScriptRoot
$manifest=Get-Content -LiteralPath (Join-Path $root 'docs/licenses/source-manifest.json') -Raw|ConvertFrom-Json
$source=Join-Path $PackageDirectory 'sources'
$cache=Join-Path $root 'work/upstream-sources'
New-Item -ItemType Directory -Force $source,$cache|Out-Null
$records=[Collections.Generic.List[object]]::new()
foreach($component in $manifest.components){
 $download=Join-Path $cache ($component.Name+'-'+$component.Commit+'.zip')
 if(-not (Test-Path -LiteralPath $download)){
  Invoke-WebRequest -Uri ('https://codeload.github.com/'+$component.Repository+'/zip/'+$component.Commit) -OutFile $download
 }
 if((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $component.Sha256){throw ('Upstream source checksum mismatch: '+$component.Name)}
 $target=Join-Path $source ($component.Name+'-'+$component.Commit+'.source.zip')
 $inputZip=[IO.Compression.ZipFile]::OpenRead($download)
 $outputZip=[IO.Compression.ZipFile]::Open($target,[IO.Compression.ZipArchiveMode]::Create)
 try{
  foreach($entry in $inputZip.Entries){
   if($entry.FullName.EndsWith('/')){continue}
   # Keep preferred source, signed LGPL module resources and the Pawn compiler source RPM.
   # Unrelated ready-made installers and build-tool binaries are not redistributed here.
   if($entry.FullName -match '\.(exe|dll|sys|nupkg)$' -or ($entry.FullName -match '\.rpm$' -and $entry.FullName -notmatch '\.src\.rpm$')){continue}
   $copy=$outputZip.CreateEntry($entry.FullName,[IO.Compression.CompressionLevel]::Optimal)
   $a=$entry.Open();$b=$copy.Open()
   try{$a.CopyTo($b)}finally{$a.Dispose();$b.Dispose()}
  }
 }finally{$inputZip.Dispose();$outputZip.Dispose()}
 $records.Add([ordered]@{Component=$component.Name;Repository=$component.Repository;Commit=$component.Commit;OriginalArchiveSha256=$component.Sha256;IncludedSourceArchive=[IO.Path]::GetFileName($target);IncludedSha256=(Get-FileHash -LiteralPath $target).Hash})
}
Push-Location $root
try{
 if(@(git status --porcelain).Count){throw 'Commit the source tree before packaging so binary and corresponding source match.'}
 $revision=git rev-parse HEAD
 $target=Join-Path $source 'AIO-Display-source.zip'
 $zip=[IO.Compression.ZipFile]::Open($target,[IO.Compression.ZipArchiveMode]::Create)
 try{
  foreach($relative in @(git ls-files)){
   if($relative -match '^Archiv-' -or $relative -match '\.(exe|dll|sys)$'){continue}
   [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $relative),('AIO-Display/'+$relative),[IO.Compression.CompressionLevel]::Optimal)|Out-Null
  }
 }finally{$zip.Dispose()}
 $records.Add([ordered]@{Component='AIO Display';Commit=$revision;IncludedSourceArchive='AIO-Display-source.zip';IncludedSha256=(Get-FileHash -LiteralPath $target).Hash})
}finally{Pop-Location}
$records|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $source 'SOURCE-MANIFEST.json') -Encoding utf8
@'
CORRESPONDING SOURCES
AIO-Display-source.zip contains the current application, contracts, sensor host, tests,
assets, dependency lockfiles and build/packaging scripts at SOURCE-MANIFEST.json's revision.
Build it with the .NET SDK in global.json: dotnet build AioDisplay.sln -c Release.
The system SDK and NuGet packages can be restored normally; licenses remain in licenses/.
Historical binary backups are not required for this build and are excluded.

The other archives contain the upstream sources of the exact MPL/LGPL versions used.
For LHM build LibreHardwareMonitorLib/LibreHardwareMonitorLib.csproj, not its separate GUI.
Its third-party notice and signed PawnIO module provenance are included under licenses/.
PawnIO.Modules 0.1.6 source and its compiler source RPM are provided separately.
Upstream's .github/workflows/ci.yml records the Pawn build command. To rebuild the
compiler first, use rpmbuild --rebuild _pawn/*.src.rpm on the specified Oracle Linux 9
build environment, then install the resulting pawn RPM and run the recorded pawncc commands.
An unsigned rebuilt module is not a replacement signature from the upstream author.
Library/application sources can be rebuilt and replaced in the extracted app directory;
no obfuscation, app signature check or project EULA prohibits debugging such modifications.

Original upstream archive hashes and redistributed source-archive hashes are both recorded.
Unrelated prebuilt installers and compiler binaries were omitted; library/module source,
license files, build scripts, module resources and compiler source packages were preserved.
'@|Set-Content -LiteralPath (Join-Path $source 'README-SOURCES.txt') -Encoding utf8
