$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot
Push-Location $projectRoot
try {
 New-Item -ItemType Directory -Force work | Out-Null
 $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 $sources=@(Get-ChildItem -LiteralPath $projectRoot -Filter '*.cs' -File | ForEach-Object FullName)
 foreach($test in @('Check','SettingsCheck')){
  & $compiler /nologo /target:exe "/main:$test" "/out:work\$test.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll "/resource:App.ico,AioDisplay.Icon" @sources (Join-Path $PSScriptRoot "$test.cs")
  if($LASTEXITCODE -ne 0){throw "Build fehlgeschlagen: $test"}
  & (Join-Path $projectRoot "work\$test.exe")
  if($LASTEXITCODE -ne 0){throw "Test fehlgeschlagen: $test"}
 }
} finally {Pop-Location}
