param([Parameter(Mandatory=$true)][string]$RetiredDirectory,[Parameter(Mandatory=$true)][string]$BackupDirectory)
$ErrorActionPreference='Stop'
$retired=[IO.Path]::GetFullPath($RetiredDirectory)
$backup=[IO.Path]::GetFullPath($BackupDirectory)
$current=Join-Path (Split-Path $retired) 'AIO-Display\BLUB-Display.exe'
if((Split-Path $retired -Leaf) -ne 'BLUB-Standalone' -or -not (Test-Path -LiteralPath $current)){throw 'Unexpected installation layout'}
$target=Join-Path $retired 'BLUB-Display.exe'
if(@(Get-Process -Name BLUB-Display -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq $target}).Count){throw 'Retired application must be stopped first'}
New-Item -ItemType Directory -Path $backup -Force|Out-Null
$original=Join-Path $backup 'BLUB-Display-original.exe'
if(Test-Path -LiteralPath $original){throw 'Existing backup must not be overwritten'}
Copy-Item -LiteralPath $target -Destination $original
Get-FileHash -LiteralPath $original -Algorithm SHA256|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $backup 'original-sha256.json')
$built=Join-Path $backup 'BLUB-Display-launcher.exe'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll "/out:$built" (Join-Path $PSScriptRoot 'LegacyLauncher.cs')
if($LASTEXITCODE -ne 0){throw 'Compatibility launcher build failed'}
Copy-Item -LiteralPath $built -Destination $target -Force
@("Dieser Pfad startet nur noch die aktuelle Anwendung:",$current,"Originaldatei gesichert unter:",$original)|Set-Content -LiteralPath (Join-Path $retired 'WEITERLEITUNG.txt')
[pscustomobject]@{RetiredEntry=$target;CurrentApplication=$current;OriginalBackup=$original;LauncherSHA256=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash}|ConvertTo-Json
