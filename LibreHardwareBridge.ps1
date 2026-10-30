param([int]$ParentId=0,[switch]$Once,[int]$Samples=0,[ValidateSet(100,125,250,500,1000)][int]$IntervalMs=250)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SnapshotWriter.ps1')
. (Join-Path $PSScriptRoot 'DependencyPaths.ps1')
$base=Find-LibreHardwareRoot
foreach($dll in @('HidSharp.dll','LibreHardwareMonitorLib.dll')){$path=Join-Path $base $dll;if(Test-Path -LiteralPath $path){[Reflection.Assembly]::LoadFrom($path)|Out-Null}}
# Older LHM uses Data in GiB and SmallData in MiB; newer releases use bytes.
$legacyUnits=[Enum]::GetNames([LibreHardwareMonitor.Hardware.SensorType]) -contains 'SmallData'
function MemorySensor($hardware,$name){$hardware.Sensors|Where-Object {$_.Name -eq $name -and $_.SensorType.ToString() -in @('Data','SmallData')}|Select-Object -First 1}
function MemoryDivisor($sensor){if(-not $legacyUnits){return 1048576};if($sensor -and $sensor.SensorType.ToString() -eq 'SmallData'){return 1};return (1.0/1024)}
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
  $cpus=@($pc.Hardware|Where-Object {$_.HardwareType.ToString() -eq 'Cpu'})
  $ram=$pc.Hardware|Where-Object {$_.HardwareType.ToString() -eq 'Memory' -and @($_.Sensors | Where-Object {$_.Identifier.ToString().StartsWith('/ram/')}).Count -gt 0}|Select-Object -First 1
  $gpus=@($pc.Hardware|Where-Object {$_.HardwareType.ToString() -match '^Gpu'})
  $gpu=$gpus|Sort-Object -Property @{Expression={$m=MemorySensor $_ 'GPU Memory Total';if($m -and $m.Value){[double]$m.Value/(MemoryDivisor $m)}else{0}}} -Descending|Select-Object -First 1
  if($displaySettings['GpuIdentifier']){$selected=$gpus|Where-Object {$_.Identifier.ToString() -eq $displaySettings['GpuIdentifier']}|Select-Object -First 1;if($selected){$gpu=$selected}}
  $threadCount=0;$cpuLoads=[Collections.Generic.List[double]]::new();$cpuTemps=[Collections.Generic.List[double]]::new()
  foreach($cpu in $cpus){
   $threads=@($cpu.Sensors|Where-Object {$_.SensorType.ToString() -eq 'Load' -and $_.Name -match '^CPU (Core|Thread) #\d+'}|Sort-Object Index)
   foreach($sensor in $threads){$threadCount++;AddReading $rows 144 ($threadCount-1) ('CPU'+$threadCount+' usage') '%' $sensor}
   $load=Sensor $cpu 'CPU Total' 'Load';if($load -and $null -ne $load.Value){for($i=0;$i -lt [Math]::Max(1,$threads.Count);$i++){$cpuLoads.Add([double]$load.Value)}}
   $temp=$null
   foreach($name in @('Core (Tctl/Tdie)','CPU Package','CPU (Tctl/Tdie)','Core (Tdie)')){$candidate=Sensor $cpu $name 'Temperature';if($candidate -and $candidate.Value -gt 0 -and $candidate.Value -lt 150){$temp=$candidate;break}}
   if(-not $temp){$temp=$cpu.Sensors|Where-Object {$_.SensorType.ToString() -eq 'Temperature' -and $_.Value -gt 0 -and $_.Value -lt 150}|Sort-Object Value -Descending|Select-Object -First 1}
   if($temp){$cpuTemps.Add([double]$temp.Value)}
  }
  if($cpuLoads.Count){AddReading $rows 144 4294967295 'CPU usage' '%' ([pscustomobject]@{Value=($cpuLoads|Measure-Object -Average).Average})}
  if($cpuTemps.Count){AddReading $rows 128 4294967295 'CPU temperature' 'C' ([pscustomobject]@{Value=($cpuTemps|Measure-Object -Maximum).Maximum})}
  if($ram){AddReading $rows 145 0 'RAM usage' 'MB' ($m=MemorySensor $ram 'Memory Used') (MemoryDivisor $m)}
  $gpuName='';$vramTotal=''
  if($gpu){
   $gpuName=$gpu.Name
   AddReading $rows 0 0 'GPU temperature' 'C' (Sensor $gpu 'GPU Core' 'Temperature')
   $gpuLoad=Sensor $gpu 'GPU Core' 'Load';if(-not $gpuLoad){$gpuLoad=Sensor $gpu 'D3D 3D' 'Load'}
   AddReading $rows 48 0 'GPU usage' '%' $gpuLoad
   AddReading $rows 49 0 'GPU1 memory usage' 'MB' ($m=MemorySensor $gpu 'GPU Memory Used') (MemoryDivisor $m)
   AddReading $rows 17 0 'GPU1 fan tachometer' 'RPM' (Sensor $gpu 'GPU Fan' 'Fan')
   $total=MemorySensor $gpu 'D3D Dedicated Memory Total';if(-not $total){$total=MemorySensor $gpu 'GPU Memory Total'}
   if($total -and $total.Value -gt 0){$vramTotal=([double]$total.Value/(MemoryDivisor $total)).ToString('R',$invariant)}
  }
  if($sequence -eq 0){$catalog=[Collections.Generic.List[string]]::new();foreach($card in $gpus){$catalog.Add($card.Identifier.ToString()+[char]9+$card.Name.Replace([char]9,[char]32))};Publish-Snapshot (Join-Path $PSScriptRoot 'hardware-catalog.tsv') $catalog | Out-Null}
  $sequence++;$lines=[Collections.Generic.List[string]]::new();$lines.Add([DateTime]::UtcNow.Ticks.ToString()+[char]9+$sequence+[char]9+$gpuName+[char]9+$vramTotal+[char]9+$threadCount);$lines.AddRange($rows)
  $target=Join-Path $PSScriptRoot 'lhm-sensors.tsv';Publish-Snapshot $target $lines | Out-Null
  if($Once -or ($Samples -gt 0 -and $sequence -ge $Samples)){break}
  $nextDue+=$IntervalMs;$remaining=$nextDue-$clock.ElapsedMilliseconds;if($remaining -gt 0){Start-Sleep -Milliseconds $remaining}elseif($remaining -lt -$IntervalMs){$nextDue=$clock.ElapsedMilliseconds}
 }while($true)
}finally{$pc.Close()}
