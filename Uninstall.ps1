$ErrorActionPreference = 'Stop'
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$taskName = "AutoMonitorBrightness-Shared-$sid"
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $taskName -ErrorAction Stop
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction Stop
}
Write-Host 'Automatic brightness disabled. Current brightness is retained.'
Write-Host 'You may delete the AutoMonitorBrightness folder in your LocalAppData after uninstalling.'
