$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot
$buildWork=Join-Path $projectRoot 'work'
New-Item -ItemType Directory -Force $buildWork | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$iconBuilder=Join-Path $buildWork 'BuildIcon.exe'
& $compiler /nologo "/out:$iconBuilder" /r:System.Drawing.dll (Join-Path $PSScriptRoot 'BuildIcon.cs')
if($LASTEXITCODE -ne 0){throw 'Icon-Build fehlgeschlagen'}
& $iconBuilder (Join-Path $projectRoot 'assets')
if($LASTEXITCODE -ne 0){throw 'Icon-Erstellung fehlgeschlagen'}
