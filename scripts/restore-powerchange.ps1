$ErrorActionPreference = 'Stop'
$source = 'E:\Data\Hermes File\projects\PowerProfile\backup\PowerChange'
$target = 'C:\Program Files\PowerChange'
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item "$source\RunHidden.vbs" "$target\RunHidden.vbs" -Force
Copy-Item "$source\PowerProfileSwitch.ps1" "$target\PowerProfileSwitch.ps1" -Force
Copy-Item "$source\RefreshRateChanger.exe" "$target\RefreshRateChanger.exe" -Force
Copy-Item "$source\PowerProfileLauncher.exe" "$target\PowerProfileLauncher.exe" -Force
Write-Output 'POWERCHANGE_RESTORED'
Get-ChildItem $target | Select-Object Name,Length
