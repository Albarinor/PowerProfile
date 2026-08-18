$ErrorActionPreference = 'Stop'
$paths = @(
    'E:\Data\Hermes File\projects\PowerProfile\scripts\dist\PowerProfile.App\PowerProfile.exe',
    'E:\Data\Hermes File\projects\PowerProfile\scripts\dist\PowerProfile-Setup-1.0.0.exe'
)
Add-Type -AssemblyName System.Drawing
foreach ($path in $paths) {
    $file = Get-Item -LiteralPath $path
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($path)
    Write-Output "PATH=$path"
    Write-Output "SIZE=$($file.Length)"
    Write-Output "SHA256=$hash"
    Write-Output "ICON=$($icon.Width)x$($icon.Height)"
    $icon.Dispose()
}
