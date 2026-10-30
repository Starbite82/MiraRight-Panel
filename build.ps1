param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
& $Dotnet build (Join-Path $PSScriptRoot 'AioDisplay.sln') -c Release -p:RestoreLockedMode=true
if($LASTEXITCODE -ne 0){throw 'Build failed'}
