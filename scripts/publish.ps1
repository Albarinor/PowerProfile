param(
    [string]$Configuration = 'Release',
    [string]$Output = "$PSScriptRoot\dist"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$inno = 'C:\Program Files\Inno Setup 7\ISCC.exe'
if (-not (Test-Path $inno)) { throw "Inno Setup compiler not found: $inno" }

Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$appOut = Join-Path $Output 'PowerProfile.App'
$flutterOut = Join-Path $Output 'PowerProfile.FlutterUI'
$nuget = Join-Path $root 'NuGet.clean.config'

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$root\PowerProfile.FlutterUI\verify-windows.ps1"
if ($LASTEXITCODE -ne 0) { throw "Flutter verification failed: $LASTEXITCODE" }

$flutterProject = Join-Path $root 'PowerProfile.FlutterUI'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '$flutterProject'; flutter build windows --release"
if ($LASTEXITCODE -ne 0) { throw "Flutter release build failed: $LASTEXITCODE" }

& dotnet publish "$root\PowerProfile.App\PowerProfile.App.csproj" --configfile $nuget -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $appOut
if ($LASTEXITCODE -ne 0) { throw "App publish failed: $LASTEXITCODE" }

$flutterRelease = Join-Path $root 'PowerProfile.FlutterUI\build\windows\x64\runner\Release'
if (-not (Test-Path (Join-Path $flutterRelease 'powerprofile_flutter_ui.exe'))) { throw "Flutter release executable not found: $flutterRelease" }
Copy-Item $flutterRelease $flutterOut -Recurse -Force

& $inno "$root\installer\PowerProfile.iss"
if ($LASTEXITCODE -ne 0) { throw "Installer build failed: $LASTEXITCODE" }

$installer = Join-Path $Output 'PowerProfile-Setup-1.0.0.exe'
if (-not (Test-Path $installer)) { throw "Installer artifact not found: $installer" }
Write-Output "PUBLISH_OK=$installer"
