# PowerProfileSwitch.ps1
$battery = Get-CimInstance -ClassName Win32_Battery -ErrorAction SilentlyContinue
if ($battery) { $onBattery = ($battery.BatteryStatus -eq 1) } else { $onBattery = $false }
$refreshRate = if ($onBattery) { 60 } else { 90 }
$enableAnimations = (-not $onBattery)
$changer = "C:\Program Files\PowerChange\RefreshRateChanger.exe"
Start-Process -FilePath $changer -ArgumentList $refreshRate -Wait -NoNewWindow | Out-Null
$aniVal = if ($enableAnimations) { 1 } else { 0 }
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "TaskbarAnimations" -Value $aniVal -ErrorAction SilentlyContinue
Set-ItemProperty -Path "HKCU:\Control Panel\Desktop\WindowMetrics" -Name "MinAnimate" -Value ([string]$aniVal) -ErrorAction SilentlyContinue
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class W32Ani {
    [StructLayout(LayoutKind.Sequential)] public struct ANIMATIONINFO { public uint cbSize; public int iMinAnimate; }
    [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a,uint b,ref ANIMATIONINFO c,uint d);
    [DllImport("user32.dll")] public static extern bool SystemParametersInfoInt(uint a,uint b,int c,uint d);
}
"@
$ai = New-Object W32Ani+ANIMATIONINFO
$ai.cbSize = [Runtime.InteropServices.Marshal]::SizeOf($ai); $ai.iMinAnimate = $aniVal
[W32Ani]::SystemParametersInfo(0x0049,$ai.cbSize,[ref]$ai,3) | Out-Null
foreach ($s in @(0x1004,0x1006,0x1002,0x1014,0x1016,0x1018,0x101A,0x103F,0x1042)) { [W32Ani]::SystemParametersInfoInt($s,0,$aniVal,3) | Out-Null }
Add-Type -MemberDefinition "[DllImport(""user32.dll"")] public static extern IntPtr SendMessageTimeout(IntPtr h,uint m,UIntPtr w,string l,uint f,uint t,out UIntPtr r);" -Namespace NM -Name NM -ErrorAction SilentlyContinue
[UIntPtr]$r=[UIntPtr]::Zero; [NM.NM]::SendMessageTimeout([intptr]0xffff,0x1A,[UIntPtr]::Zero,"Software",2,100,[ref]$r) | Out-Null
$mode = if ($onBattery){"BATTERY"}else{"AC POWER"}
Write-Host "[OK] $mode -> ${refreshRate}Hz, Animations: $(if($enableAnimations){"ON"}else{"OFF"})"

