param([int]$ParentId=0,[switch]$Once)
$ErrorActionPreference='Stop'
$fanRoot='C:\Program Files (x86)\FanControl'
foreach($dll in @('Google.Protobuf.dll','Grpc.Core.Api.dll','GrpcDotNetNamedPipes.dll','FanControl.IPC.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $fanRoot $dll))|Out-Null}
$client=[FanControl.IPC.IPCFactory]::GetSensorClient()
$configuration=Get-Content (Join-Path $fanRoot 'Configurations\userConfig.json') -Raw | ConvertFrom-Json
$sourceRows=@($configuration.FanControl.FanSensors | Where-Object {-not $_.IsHidden})
while($true){
 if($ParentId -gt 0 -and -not (Get-Process -Id $ParentId -ErrorAction SilentlyContinue)){break}
 $values=@{}
 try{
  $reply=$client.GetAllSensors([GetAllSensorsRequest]::new(),$null,[DateTime]::UtcNow.AddMilliseconds(800),[Threading.CancellationToken]::None)
  foreach($sensor in $reply.Sensors){if($sensor.HasValue){$values[$sensor.Identifier]=$sensor.Value}}
 }catch{}
 $lines=[Collections.Generic.List[string]]::new();$lines.Add([DateTime]::UtcNow.Ticks.ToString())
 foreach($source in $sourceRows){
  $paired=$configuration.FanControl.Controls | Where-Object {$_.PairedFanSensor.Identifier -eq $source.Identifier} | Select-Object -First 1
  $name=if($paired){$paired.NickName}else{$source.NickName}
  $name=$name.Replace([char]9,[char]32).Replace([char]10,[char]32)
  $rpm=if($values.ContainsKey($source.Identifier)){$values[$source.Identifier].ToString('0',[Globalization.CultureInfo]::InvariantCulture)}else{''}
  $lines.Add($source.Identifier+[char]9+$name+[char]9+$rpm)
 }
 $target=Join-Path $PSScriptRoot 'fan-rpm.tsv';$temp=$target+'.tmp'
 [IO.File]::WriteAllLines($temp,$lines,[Text.UTF8Encoding]::new($false))
 [IO.File]::Move($temp,$target,$true)
 if($Once){break};Start-Sleep -Milliseconds 1000
}
