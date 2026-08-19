$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'build\windows\x64\runner\Debug\powerprofile_flutter_ui.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "Flutter Windows build not found: $exe. Run .\verify-windows.ps1 first."
}

$p = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
Start-Sleep -Seconds 3
Write-Output "PID=$($p.Id)"
Write-Output "RUNNING=$(-not $p.HasExited)"
if (-not $p.HasExited) {
    $p.CloseMainWindow() | Out-Null
    Start-Sleep -Seconds 1
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }
}
