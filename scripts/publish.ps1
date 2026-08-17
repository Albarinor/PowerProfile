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
$nuget = Join-Path $root 'NuGet.clean.config'

& dotnet publish "$root\PowerProfile.App\PowerProfile.App.csproj" --configfile $nuget -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $appOut
if ($LASTEXITCODE -ne 0) { throw "App publish failed: $LASTEXITCODE" }

& $inno "$root\installer\PowerProfile.iss"
if ($LASTEXITCODE -ne 0) { throw "Installer build failed: $LASTEXITCODE" }

Write-Output "PUBLISH_OK=$Output"
