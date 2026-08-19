$ErrorActionPreference = 'Stop'
$project = (Resolve-Path $PSScriptRoot).Path
$flutter = if ($env:FLUTTER_EXE) { $env:FLUTTER_EXE } else { 'C:\ProgramData\flutter\bin\flutter.bat' }
if (-not (Test-Path -LiteralPath $flutter -PathType Leaf)) {
    throw "Flutter executable not found: $flutter. Set FLUTTER_EXE or install Flutter at C:\ProgramData\flutter."
}

Push-Location $project
try {
    & $flutter pub get
    & $flutter analyze
    & $flutter test
    & $flutter build windows --debug
} finally {
    Pop-Location
}
