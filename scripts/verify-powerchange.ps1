$ErrorActionPreference = 'Stop'
$tokens = $null
$errors = $null
[System.Management.Automation.Language.Parser]::ParseFile('C:\Program Files\PowerChange\PowerProfileSwitch.ps1', [ref]$tokens, [ref]$errors) | Out-Null
if ($errors.Count -eq 0) { Write-Output 'POWERSHELL_PARSE_OK' } else { $errors | Format-List | Out-String | Write-Error }
