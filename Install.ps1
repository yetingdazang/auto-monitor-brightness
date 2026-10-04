$ErrorActionPreference = 'Stop'
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$taskName = "AutoMonitorBrightness-Shared-$sid"
$installFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AutoMonitorBrightness'
$configPath = Join-Path $PSScriptRoot 'settings.ini'
$values = @{}
foreach ($raw in (Get-Content -LiteralPath $configPath -ErrorAction Stop)) {
    $line = $raw.Trim()
    if (!$line -or $line.StartsWith('#')) { continue }
    $parts = $line -split '=', 2
    if ($parts.Count -ne 2 -or $values.ContainsKey($parts[0].Trim())) { throw 'Invalid settings.ini' }
    $values[$parts[0].Trim()] = $parts[1].Trim()
}
$day = [DateTime]::ParseExact($values.DayTime, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture)
$night = [DateTime]::ParseExact($values.NightTime, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture)
if ($day -ge $night) { throw 'DayTime must be earlier than NightTime.' }
foreach ($key in @('DayBrightness', 'NightBrightness')) {
    $brightness = [int]::Parse($values[$key], [Globalization.CultureInfo]::InvariantCulture)
    if ($brightness -lt 0 -or $brightness -gt 100) { throw 'Brightness must be 0 to 100.' }
}
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $taskName -ErrorAction Stop
}
New-Item -ItemType Directory -Path $installFolder -Force -ErrorAction Stop | Out-Null
foreach ($file in @('AutoBrightness.exe', 'settings.ini', 'Install.ps1', 'Uninstall.ps1', 'Install.cmd', 'Uninstall.cmd', '说明.txt')) {
    $source = Join-Path $PSScriptRoot $file
    $destination = Join-Path $installFolder $file
    if ([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($destination)) {
        Copy-Item -LiteralPath $source -Destination $destination -Force -ErrorAction Stop
    }
}
$exe = Join-Path $installFolder 'AutoBrightness.exe'
$action = New-ScheduledTaskAction -Execute $exe -WorkingDirectory $installFolder -ErrorAction Stop
$triggers = @(
    (New-ScheduledTaskTrigger -Daily -At $day -ErrorAction Stop)
    (New-ScheduledTaskTrigger -Daily -At $night -ErrorAction Stop)
    (New-ScheduledTaskTrigger -AtLogOn -User $user -ErrorAction Stop)
)
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited -ErrorAction Stop
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 2) -ErrorAction Stop
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $triggers -Principal $principal -Settings $settings -Description 'Automatic monitor brightness at daytime, nighttime and user logon. Uses local PC time.' -Force -ErrorAction Stop | Out-Null
Start-ScheduledTask -TaskName $taskName -ErrorAction Stop
Write-Host "Installed: $installFolder"
Write-Host "Day: $($values.DayTime) -> $($values.DayBrightness)% ; Night: $($values.NightTime) -> $($values.NightBrightness)%"
Write-Host 'Use brightness.log to check monitor support and results.'
