param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class StartupWindows {
  public delegate bool Callback(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] static extern bool EnumWindows(Callback cb, IntPtr p);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  public static int Count(int pid) {
    int count=0;
    EnumWindows((h,p)=>{uint id; GetWindowThreadProcessId(h,out id); if(id==pid && IsWindowVisible(h)) count++; return true;},IntPtr.Zero);
    return count;
  }
}
'@
$before = @(Get-Process powerprofile_flutter_ui -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
$p = $null
try {
  $p = Start-Process -FilePath $Executable -ArgumentList '--silent' -PassThru
  $deadline = [DateTime]::UtcNow.AddSeconds(6)
  do {
    Start-Sleep -Milliseconds 100
    $p.Refresh()
    if ($p.HasExited) {throw 'Silent backend exited.'}
    if ([StartupWindows]::Count($p.Id) -ne 0) {throw 'Silent startup displayed a native window.'}
    $newUi = @(Get-Process powerprofile_flutter_ui -ErrorAction SilentlyContinue | Where-Object {$_.Id -notin $before})
    if ($newUi.Count -ne 0) {throw 'Silent startup launched the Flutter dashboard.'}
  } while ([DateTime]::UtcNow -lt $deadline)
  Write-Output 'PASS: --silent stays alive without a visible native window or Flutter process.'
} finally {
  if ($p -and -not $p.HasExited) {Stop-Process -Id $p.Id -Force}
  Get-Process powerprofile_flutter_ui -ErrorAction SilentlyContinue | Where-Object {$_.Id -notin $before} | Stop-Process -Force
}
