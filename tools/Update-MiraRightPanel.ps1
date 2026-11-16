# SPDX-License-Identifier: GPL-3.0-only
[CmdletBinding(DefaultParameterSetName='Update')]
param(
    [Parameter(Mandatory, ParameterSetName='Update')][string]$Package,
    [Parameter(Mandatory, ParameterSetName='Update')][ValidatePattern('^[a-fA-F0-9]{64}$')][string]$Sha256,
    [Parameter(ParameterSetName='Update')][string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\MiraRight Panel'),
    [Parameter(Mandatory, ParameterSetName='ProtectedWorker')][ValidateSet('Install','Restore')][string]$ProtectedWorker,
    [Parameter(Mandatory, ParameterSetName='ProtectedWorker')][guid]$Transaction,
    [Parameter(ParameterSetName='ProtectedWorker')][string]$VerifiedPackage
)
$ErrorActionPreference = 'Stop'

if ($PSCmdlet.ParameterSetName -eq 'ProtectedWorker') {
    $root = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'MiraRight Panel'
    $current = Join-Path $root 'Sensors'
    $backup = Join-Path $root "Sensors-UpdateBackup-$Transaction"
    $candidate = Join-Path $root "Sensors-UpdateStage-$Transaction"
    foreach ($path in $root,$current,$backup,$candidate) {
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unerwartete Verknüpfung: $path"
        }
    }
    if ($ProtectedWorker -eq 'Restore') {
        if (Test-Path -LiteralPath $current) { Move-Item -LiteralPath $current -Destination "$backup.failed" }
        Move-Item -LiteralPath $backup -Destination $current
        return
    }
    & (Join-Path $PSScriptRoot 'Test-MiraRightPackage.ps1') -PackageDirectory $VerifiedPackage | Out-Null
    Copy-Item -LiteralPath (Join-Path $VerifiedPackage 'sensors') -Destination $candidate -Recurse
    # The task continues to execute the same fixed, administrator-protected path.
    Move-Item -LiteralPath $current -Destination $backup
    try { Move-Item -LiteralPath $candidate -Destination $current }
    catch {
        Move-Item -LiteralPath $backup -Destination $current
        throw
    }
    return
}

function Stop-Panel([Diagnostics.Process]$Process, [string]$Executable) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    Start-Process -FilePath $Executable -ArgumentList '--stop' -Wait -WindowStyle Hidden
    if (!$Process.WaitForExit(15000)) {
        $Process.Kill()
        if (!$Process.WaitForExit(5000)) { throw 'Das alte Panel konnte nicht beendet werden.' }
    }
}

function Wait-PanelHealthy([Diagnostics.Process]$Process, [string]$StateFile) {
    $started = $Process.StartTime.ToUniversalTime().Ticks
    $deadline = [Diagnostics.Stopwatch]::StartNew()
    $first = 0
    while ($deadline.Elapsed.TotalSeconds -lt 45 -and !$Process.HasExited) {
        $state = $null
        try { $state = Get-Content -LiteralPath $StateFile -Raw | ConvertFrom-Json }
        catch { }
        if ($state.ProcessId -eq $Process.Id -and $state.SensorProcessId -and $state.SensorSampleTicks -ge $started) {
            if ($first -and $state.SensorSampleTicks -gt $first) { return }
            $first = $state.SensorSampleTicks
        }
        Start-Sleep -Milliseconds 500
    }
    throw 'Die neue Installation liefert keine fortlaufenden Sensordaten.'
}

function Get-ScheduledSensorPath {
    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $service = New-Object -ComObject Schedule.Service
    $service.Connect()
    try { $task = $service.GetFolder('\').GetTask("MiraRight Panel Sensors-$sid") }
    catch [Runtime.InteropServices.COMException] {
        if ($_.Exception.HResult -eq -2147024894) { return }
        throw
    }
    $expected = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'MiraRight Panel\Sensors\MiraRightPanel.SensorHost.exe'
    if ($task.Definition.Actions.Count -ne 1 -or $task.Definition.Actions.Item(1).Path -ine $expected) {
        throw 'Die registrierte MiraRight-Sensoraufgabe verwendet einen unerwarteten Pfad.'
    }
    Split-Path -Parent $expected
}

function Set-ProtectedWorker([string]$Action) {
    $powerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $arguments = @('-NoProfile', '-File', ('"{0}"' -f $PSCommandPath),
        '-ProtectedWorker', $Action, '-Transaction', $transaction, '-VerifiedPackage', ('"{0}"' -f $candidate))
    $helper = Start-Process -FilePath $powerShell -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
    if ($helper.ExitCode -ne 0) { throw "Geschützter Sensorprozess: $Action fehlgeschlagen." }
}

$install = (Resolve-Path -LiteralPath $InstallDirectory).Path
if ((Get-Item -LiteralPath $install).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Der Installationsordner ist eine Verknüpfung.' }
$exe = Join-Path $install 'MiraRightPanel.exe'
if (!(Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Keine MiraRight-Installation unter $install." }
$lockPath = "$install.update.lock"
$lock = [IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None')
try {
    $packagePath = (Resolve-Path -LiteralPath $Package).Path
    if ((Get-FileHash -LiteralPath $packagePath).Hash -ne $Sha256) { throw 'Die ZIP-Prüfsumme stimmt nicht.' }
    $transaction = [guid]::NewGuid()
    $stage = "$install.update-$transaction"
    $backup = "$install.previous-$transaction"
    Expand-Archive -LiteralPath $packagePath -DestinationPath $stage
    $candidate = Join-Path $stage 'MiraRight-Panel'
    & (Join-Path $PSScriptRoot 'Test-MiraRightPackage.ps1') -PackageDirectory $candidate | Out-Null
    $session = [Diagnostics.Process]::GetCurrentProcess().SessionId
    $running = @(Get-Process -Name MiraRightPanel -ErrorAction SilentlyContinue | Where-Object SessionId -eq $session)
    if ($running.Count -gt 1 -or ($running.Count -eq 1 -and $running[0].Path -ine $exe)) {
        throw 'Bitte beende die andere laufende MiraRight-Instanz vor dem Update.'
    }
    $protectedPath = Get-ScheduledSensorPath
    $wasRunning = $running.Count -eq 1
    $protectedUpdated = $false
    $switched = $false
    $newPanel = $null
    try {
        if ($wasRunning) { Stop-Panel $running[0] $exe }
        if ($protectedPath) {
            $protectedBackup = Join-Path (Split-Path -Parent $protectedPath) "Sensors-UpdateBackup-$transaction"
            try { Set-ProtectedWorker Install }
            finally { $protectedUpdated = Test-Path -LiteralPath $protectedBackup }
        }
        foreach ($name in 'settings.ini','displays.json','legacy-flexpanel.json','fan-names.json') {
            $file = Join-Path $install $name
            if (Test-Path -LiteralPath $file -PathType Leaf) { Copy-Item -LiteralPath $file -Destination $candidate -Force }
        }
        Move-Item -LiteralPath $install -Destination $backup
        Move-Item -LiteralPath $candidate -Destination $install
        $switched = $true
        $newPanel = Start-Process -FilePath $exe -ArgumentList '--minimized' -WorkingDirectory $install -WindowStyle Hidden -PassThru
        Wait-PanelHealthy $newPanel (Join-Path $install 'dual-state.json')
        "MiraRight aktualisiert. Vorherige Installation: $backup"
    }
    catch {
        $failure = $_
        # Keep the failed candidate and previous installation available for diagnosis.
        if ($newPanel) { Stop-Panel $newPanel $exe }
        if ($switched) { Move-Item -LiteralPath $install -Destination (Join-Path $stage 'failed') }
        if (Test-Path -LiteralPath $backup) { Move-Item -LiteralPath $backup -Destination $install }
        if ($protectedUpdated) { Set-ProtectedWorker Restore }
        if ($wasRunning) {
            Start-Process -FilePath $exe -ArgumentList '--minimized' -WorkingDirectory $install -WindowStyle Hidden
        }
        throw $failure
    }
}
finally { $lock.Dispose() }
