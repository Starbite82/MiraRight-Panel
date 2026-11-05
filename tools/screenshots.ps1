param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
& $Dotnet run --project (Join-Path $PSScriptRoot 'Screenshots/Screenshots.csproj') -c Release -- $root
if($LASTEXITCODE -ne 0){throw 'Screenshot generation failed'}
