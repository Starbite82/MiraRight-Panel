param([int]$ParentId=0,[switch]$Once,[int]$Samples=0,[ValidateSet(100,125,250,500,1000)][int]$IntervalMs=250)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SnapshotWriter.ps1')
$base='C:\Program Files (x86)\FanControl'
foreach($dll in @('HidSharp.dll','LibreHardwareMonitorLib.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $base $dll))|Out-Null}
# This installed LHM 0.9.6+ uses SensorType.Data in bytes, not the older GiB convention.
if([Enum]::GetNames([LibreHardwareMonitor.Hardware.SensorType]) -contains 'SmallData'){throw 'Diese aeltere LHM-Einheitenkonvention wird nicht unterstuetzt.'}
$pc=[LibreHardwareMonitor.Hardware.Computer]::new()
$pc.IsCpuEnabled=$true;$pc.IsGpuEnabled=$true;$pc.IsMemoryEnabled=$true
$invariant=[Globalization.CultureInfo]::InvariantCulture
function Sensor($hardware,$name,$type){$hardware.Sensors|Where-Object {$_.Name -eq $name -and $_.SensorType.ToString() -eq $type}|Select-Object -First 1}
function AddReading($rows,[uint32]$id,[uint32]$gpu,[string]$name,[string]$unit,$sensor,[double]$divisor=1){
 if($null -eq $sensor -or $null -eq $sensor.Value){return};$value=[double]$sensor.Value/$divisor
 if([double]::IsNaN($value) -or [double]::IsInfinity($value) -or [Math]::Abs($value) -gt 1e20){return}
 if($unit -eq 'C' -and ($value -le 0 -or $value -gt 150)){return}
 $rows.Add($id.ToString()+[char]9+$gpu.ToString()+[char]9+$name+[char]9+$unit+[char]9+$value.ToString('R',$invariant))
}
try{
 $pc.Open();$sequence=0;$clock=[Diagnostics.Stopwatch]::StartNew();$nextDue=0
 do{
  if($ParentId -gt 0 -and -not (Get-Process -Id $ParentId -ErrorAction SilentlyContinue)){break}
  $watch=[Diagnostics.Stopwatch]::StartNew();$rows=[Collections.Generic.List[string]]::new()
  foreach($hardware in $pc.Hardware){$hardware.Update()}
  $cpu=$pc.Hardware|Where-Object {$_.HardwareType.ToString() -eq 'Cpu'}|Select-Object -First 1
  $ram=$pc.Hardware|Where-Object {$_.HardwareType.ToString() -eq 'Memory' -and @($_.Sensors | Where-Object {$_.Identifier.ToString().StartsWith('/ram/')}).Count -gt 0}|Select-Object -First 1
  # Match by physical memory capacity so the integrated GPU is never selected accidentally.
  $gpu=$pc.Hardware|Where-Object {$_.HardwareType.ToString() -match '^Gpu'}|Sort-Object -Property @{Expression={ $total=Sensor $_ 'GPU Memory Total' 'Data';if($total -and $total.Value){[double]$total.Value}else{0}}} -Descending|Select-Object -First 1
  if($cpu){
   AddReading $rows 128 4294967295 'CPU temperature' 'C' (Sensor $cpu 'Core (Tctl/Tdie)' 'Temperature')
   AddReading $rows 144 4294967295 'CPU usage' '%' (Sensor $cpu 'CPU Total' 'Load')
   foreach($sensor in $cpu.Sensors){if($sensor.SensorType.ToString() -eq 'Load' -and $sensor.Name -match '^CPU Core #(\d+)$'){$thread=[uint32]$Matches[1];AddReading $rows 144 ($thread-1) ('CPU'+$thread+' usage') '%' $sensor}}
  }
  if($ram){AddReading $rows 145 0 'RAM usage' 'MB' (Sensor $ram 'Memory Used' 'Data') 1048576}
  $gpuName='';$vramTotal=''
  if($gpu){
   $gpuName=$gpu.Name
   AddReading $rows 0 0 'GPU temperature' 'C' (Sensor $gpu 'GPU Core' 'Temperature')
   AddReading $rows 48 0 'GPU usage' '%' (Sensor $gpu 'GPU Core' 'Load')
   AddReading $rows 49 0 'GPU1 memory usage' 'MB' (Sensor $gpu 'GPU Memory Used' 'Data') 1048576
   AddReading $rows 17 0 'GPU1 fan tachometer' 'RPM' (Sensor $gpu 'GPU Fan' 'Fan')
   $total=Sensor $gpu 'D3D Dedicated Memory Total' 'Data';if(-not $total){$total=Sensor $gpu 'GPU Memory Total' 'Data'}
   if($total -and $total.Value -gt 0){$vramTotal=([double]$total.Value/1048576).ToString('R',$invariant)}
  }
  $sequence++;$lines=[Collections.Generic.List[string]]::new();$lines.Add([DateTime]::UtcNow.Ticks.ToString()+[char]9+$sequence+[char]9+$gpuName+[char]9+$vramTotal);$lines.AddRange($rows)
  $target=Join-Path $PSScriptRoot 'lhm-sensors.tsv';Publish-Snapshot $target $lines | Out-Null
  if($Once -or ($Samples -gt 0 -and $sequence -ge $Samples)){break}
  $nextDue+=$IntervalMs;$remaining=$nextDue-$clock.ElapsedMilliseconds;if($remaining -gt 0){Start-Sleep -Milliseconds $remaining}elseif($remaining -lt -$IntervalMs){$nextDue=$clock.ElapsedMilliseconds}
 }while($true)
}finally{$pc.Close()}
