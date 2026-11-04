# SPDX-License-Identifier: GPL-3.0-only
param(
 [string]$SensorDirectory=(Join-Path $PSScriptRoot 'sensors'),
 [string]$UserSid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value,
 [switch]$Remove
)
$ErrorActionPreference='Stop'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
if(-not ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Bitte einmal als Administrator ausführen.'}
if($UserSid -ne $identity.User.Value){throw 'Die Einrichtung muss mit dem eigenen Windows-Administratorkonto erfolgen.'}
$taskName='MiraRight Panel Sensors-'+$UserSid
$service=New-Object -ComObject Schedule.Service
$service.Connect()
$folder=$service.GetFolder('\')
if($Remove){$folder.DeleteTask($taskName,0);Write-Output 'Sensoraufgabe entfernt. UAC und Programmdateien bleiben unverändert.';return}
$source=[IO.Path]::GetFullPath($SensorDirectory)
if(-not(Test-Path -LiteralPath (Join-Path $source 'MiraRightPanel.SensorHost.exe'))){throw 'Vollständiger sensors-Ordner fehlt.'}
$root=Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'MiraRight Panel'
$target=Join-Path $root 'Sensors'
if($source.Equals($target,[StringComparison]::OrdinalIgnoreCase)){throw 'Quelle und Ziel müssen verschieden sein.'}
# Protect the fixed worker location before copying any executable files.
New-Item -ItemType Directory -Path $root -Force|Out-Null
if((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Unerwartete Verknüpfung am Installationsziel.'}
$acl=[Security.AccessControl.DirectorySecurity]::new()
$acl.SetAccessRuleProtection($true,$false)
$admins=[Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
$acl.SetOwner($admins)
foreach($entry in @(@('S-1-5-18','FullControl'),@('S-1-5-32-544','FullControl'),@('S-1-5-32-545','ReadAndExecute'))){
 $rule=[Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new($entry[0]),$entry[1],'ContainerInherit,ObjectInherit','None','Allow')
 $acl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $root -AclObject $acl
if(Test-Path -LiteralPath $target){
 if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Unerwartete Verknüpfung am Sensorziel.'}
 # Preserve an older protected installation instead of overwriting it.
 $backup=Join-Path $root ('Sensors-Backup-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
 if(-not ([IO.Path]::GetFullPath($backup)).StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Ungültiger Backuppfad'}
 Move-Item -LiteralPath $target -Destination $backup
}
New-Item -ItemType Directory -Path $target|Out-Null
if(Get-ChildItem -LiteralPath $source -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}){throw 'Verknüpfungen sind im Sensorpaket nicht erlaubt.'}
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse
$definition=$service.NewTask(0)
$definition.RegistrationInfo.Description='MiraRight Panel: startet ausschließlich den geschützten Sensorprozess für die aktuelle Panelsitzung. Keine gespeicherten Kennwörter.'
$definition.Principal.UserId=$UserSid
$definition.Principal.LogonType=3 # InteractiveToken; never stores a password
$definition.Principal.RunLevel=1 # HighestAvailable for this account
$definition.Settings.Enabled=$true
$definition.Settings.AllowDemandStart=$true
$definition.Settings.DisallowStartIfOnBatteries=$false
$definition.Settings.StopIfGoingOnBatteries=$false
$definition.Settings.ExecutionTimeLimit='PT0S'
$definition.Settings.MultipleInstances=0 # parallel handshakes are independently bound to the requesting UI
$action=$definition.Actions.Create(0)
$action.Path=Join-Path $target 'MiraRightPanel.SensorHost.exe'
$action.WorkingDirectory=$target
$action.Arguments='--pipe $(Arg0) --parent $(Arg1) --parent-start $(Arg2)'
# The owner may read and run this fixed action, but may not change it unelevated.
$sddl='O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGX;;;'+$UserSid+')'
$null=$folder.RegisterTaskDefinition($taskName,$definition,6,$UserSid,$null,3,$sddl)
Write-Output ('Sensor-Autostart eingerichtet: '+$taskName)
