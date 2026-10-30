param([int]$ParentId=0,[switch]$Once)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SnapshotWriter.ps1')
. (Join-Path $PSScriptRoot 'DependencyPaths.ps1')
$fanRoot=Find-FanControlRoot
foreach($dll in @('Google.Protobuf.dll','Grpc.Core.Api.dll','GrpcDotNetNamedPipes.dll','FanControl.IPC.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $fanRoot $dll))|Out-Null}
$client=[FanControl.IPC.IPCFactory]::GetSensorClient()
function Get-ActiveConfiguration {
 try{
  $control=[FanControl.IPC.IPCFactory]::GetFanControlClient()
  $configInfo=$control.ListAvailableConfigs([ListAvailableConfigsRequest]::new(),$null,[DateTime]::UtcNow.AddMilliseconds(800),[Threading.CancellationToken]::None)
  $path=if([IO.Path]::IsPathRooted($configInfo.CurrentConfig)){$configInfo.CurrentConfig}else{Join-Path $configInfo.ConfigFolder $configInfo.CurrentConfig}
  if(Test-Path -LiteralPath $path){return (Get-Content -LiteralPath $path -Raw|ConvertFrom-Json)}
 }catch{}
 try{
  $cache=Get-Content (Join-Path $fanRoot 'Cache.json') -Raw|ConvertFrom-Json
  $folder=if($cache.CustomConfigFolder){$cache.CustomConfigFolder}else{Join-Path $fanRoot 'Configurations'}
  $path=Join-Path $folder $cache.CurrentConfigFileName
  if(Test-Path -LiteralPath $path){return (Get-Content -LiteralPath $path -Raw|ConvertFrom-Json)}
 }catch{}
 return $null
}
$configuration=Get-ActiveConfiguration;$configAge=[DateTime]::UtcNow
while($true){
 if($ParentId -gt 0 -and -not (Get-Process -Id $ParentId -ErrorAction SilentlyContinue)){break}
 if(([DateTime]::UtcNow-$configAge).TotalSeconds -ge 30){$configuration=Get-ActiveConfiguration;$configAge=[DateTime]::UtcNow}
 $values=@{};$sensors=@()
 try{
  $reply=$client.GetAllSensors([GetAllSensorsRequest]::new(),$null,[DateTime]::UtcNow.AddMilliseconds(800),[Threading.CancellationToken]::None)
  $sensors=@($reply.Sensors)
  foreach($sensor in $sensors){if($sensor.HasValue){$values[$sensor.Identifier]=$sensor.Value}}
 }catch{}
 $lines=[Collections.Generic.List[string]]::new();$lines.Add([DateTime]::UtcNow.Ticks.ToString())
 foreach($sensor in $sensors){
  if([int]$sensor.Type -ne 2){continue}
  $source=$configuration.FanControl.FanSensors|Where-Object {$_.Identifier -eq $sensor.Identifier}|Select-Object -First 1
  if($source -and $source.IsHidden){continue}
  $paired=$configuration.FanControl.Controls|Where-Object {$_.PairedFanSensor.Identifier -eq $sensor.Identifier}|Select-Object -First 1
  $name=if($paired -and $paired.NickName){$paired.NickName}elseif($source -and $source.NickName){$source.NickName}else{$sensor.Name}
  $name=$name.Replace([char]9,[char]32).Replace([char]10,[char]32)
  $rpm=if($sensor.HasValue){$sensor.Value.ToString('0',[Globalization.CultureInfo]::InvariantCulture)}else{''}
  $lines.Add($sensor.Identifier+[char]9+$name+[char]9+$rpm)
 }
 $cpuValue=''
 $cpuSensors=@($sensors|Where-Object {$_.Identifier -match '^/(amd|intel)cpu/' -and $_.Identifier -match '/temperature/' -and $_.HasValue -and $_.Value -gt 0 -and $_.Value -lt 150})
 $cpu=$cpuSensors|Where-Object {$_.Name -match 'Tctl|Package'}|Sort-Object Value -Descending|Select-Object -First 1
 if(-not $cpu){$cpu=$cpuSensors|Sort-Object Value -Descending|Select-Object -First 1}
 if($cpu){$cpuValue=$cpu.Value.ToString('R',[Globalization.CultureInfo]::InvariantCulture)}
 $cpuTarget=Join-Path $PSScriptRoot 'fan-cpu.tsv';Publish-Snapshot $cpuTarget @([DateTime]::UtcNow.Ticks.ToString()+[char]9+$cpuValue) | Out-Null
 $target=Join-Path $PSScriptRoot 'fan-rpm.tsv';Publish-Snapshot $target $lines | Out-Null
 if($Once){break};Start-Sleep -Milliseconds 1000
}
