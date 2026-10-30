$ErrorActionPreference='Stop'
$path=Join-Path (Split-Path $PSScriptRoot) 'LibreHardwareBridge.ps1'
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors)
if($errors.Count){throw $errors[0]}
foreach($name in @('MemorySensor','MemoryDivisor','Sensor','AddReading')){
 $function=$ast.Find({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true)
 . ([scriptblock]::Create($function.Extent.Text))
}
$legacyUnits=$false
if((MemoryDivisor ([pscustomobject]@{SensorType='Data'})) -ne 1048576){throw 'Byte conversion'}
$legacyUnits=$true
if((MemoryDivisor ([pscustomobject]@{SensorType='Data'})) -ne (1.0/1024)){throw 'Legacy GiB conversion'}
if((MemoryDivisor ([pscustomobject]@{SensorType='SmallData'})) -ne 1){throw 'Legacy MiB conversion'}
$invariant=[Globalization.CultureInfo]::InvariantCulture
$rows=[Collections.Generic.List[string]]::new()
AddReading $rows 128 0 'CPU temperature' 'C' ([pscustomobject]@{Value=0})
AddReading $rows 128 0 'CPU temperature' 'C' ([pscustomobject]@{Value=65})
if($rows.Count -ne 1 -or $rows[0] -notmatch '\t65$'){throw 'Temperature validation'}
foreach($name in @('CPU Core #1','CPU Core #1 Thread #2','CPU Thread #32')){if($name -notmatch '^CPU (Core|Thread) #\d+'){throw 'Thread naming'}}
Write-Output 'PASS: modern bytes, legacy GiB/MiB, temperature validity, Intel/AMD thread naming'
