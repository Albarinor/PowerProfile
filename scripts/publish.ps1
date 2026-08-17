param(
    [string]$Configuration = 'Release',
    [string]$Output = "$PSScriptRoot\dist"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $Output | Out-Null
$appOut = Join-Path $Output 'PowerProfile.App'
$nuget = Join-Path $root 'NuGet.clean.config'

& dotnet publish "$root\PowerProfile.App\PowerProfile.App.csproj" --configfile $nuget -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $appOut
if ($LASTEXITCODE -ne 0) { throw "App publish failed: $LASTEXITCODE" }
& dotnet publish "$root\PowerProfile.Setup\PowerProfile.Setup.csproj" --configfile $nuget -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $Output
if ($LASTEXITCODE -ne 0) { throw "Setup publish failed: $LASTEXITCODE" }
Rename-Item (Join-Path $Output 'PowerProfile.Setup.exe') 'Setup.exe' -Force
Remove-Item (Join-Path $Output '*.pdb') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $appOut '*.pdb') -Force -ErrorAction SilentlyContinue
Write-Output "PUBLISH_OK=$Output"
