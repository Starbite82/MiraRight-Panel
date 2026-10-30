param([string]$Dotnet='dotnet',[switch]$Integration,[int]$LiveSeconds=240)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot
& $Dotnet build (Join-Path $root 'AioDisplay.sln') -c Release -p:RestoreLockedMode=true
if($LASTEXITCODE -ne 0){throw 'Test build failed'}
$runner=Join-Path $PSScriptRoot 'AioDisplay.Tests/bin/Release/net10.0-windows/AioDisplay.Tests.exe'
& $runner --unit (Join-Path $root 'work/tests')
if($LASTEXITCODE -ne 0){throw 'Regression tests failed'}
if($Integration){
 $hostPath=Join-Path $root 'src/AioDisplay.SensorHost/bin/Release/net10.0-windows/AioDisplay.SensorHost.exe'
 $fake=Join-Path $PSScriptRoot 'FakeSensorHost/bin/Release/net10.0-windows/FakeSensorHost.exe'
 & $runner --lifecycle (Join-Path $root 'work/lifecycle') $hostPath $fake
 if($LASTEXITCODE -ne 0){throw 'Lifecycle tests failed'}
 & $runner --live (Join-Path $root 'work/live') $hostPath $LiveSeconds
 if($LASTEXITCODE -ne 0){throw 'Live tests failed'}
}
