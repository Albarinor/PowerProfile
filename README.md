# PowerProfile

A lightweight Windows desktop utility that detects your display's available refresh rates and lets you set separate profiles for AC and battery power — with full animation control.

**Version:** 1.0.0  **Platform:** Windows 10/11  **Runtime:** .NET 10 (Windows)

---

## Features

| Feature | What it does |
|---|---|
| **Refresh Rate Control** | Detects all modes supported by your display driver via `EnumDisplaySettings`; lets you pick a target Hz for AC and battery independently |
| **Animation Control** | Toggles the full suite of Windows animations (minimize/maximize, taskbar, menu, tooltip, UI effects) via `SystemParametersInfo` and registry |
| **Auto-Switch** | Both features can auto-apply the right profile based on the current power source |
| **Apply Now** | One button applies both features immediately using the detected power state |
| **Persistent Settings** | Preferences saved to `%APPDATA%\PowerProfile\settings.json` |

---

## Requirements

- Windows 10 or 11
- [.NET 10 Runtime (Windows)](https://dotnet.microsoft.com/download/dotnet/10.0) — or use a self-contained publish (see below)
- Administrator rights are **not** required for animation changes; setting the refresh rate may need elevation on some hardware

---

## Build

### Quick build (Debug)

```powershell
dotnet build PowerProfile.App\PowerProfile.App.csproj
```

### Release publish (framework-dependent)

```powershell
.\build.ps1
```

Output: `PowerProfile.App\bin\Release\net10.0-windows\publish\`

### Self-contained (bundles .NET runtime, ~100 MB)

```powershell
.\build.ps1 -SelfContained
```

### Build + Installer (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php))

```powershell
.\build.ps1 -MakeInstaller
# Output: dist\PowerProfile-Setup-1.0.0.exe
```

---

## Install

### Option A — Inno Setup installer (recommended for distribution)

1. Run `.\build.ps1 -MakeInstaller` to produce `dist\PowerProfile-Setup-1.0.0.exe`
2. Double-click the installer; choose an install directory in the wizard
3. An optional Start Menu shortcut and startup entry are offered

### Option B — PowerShell installer (no external tools needed)

```powershell
# After running .\build.ps1
.\setup\Install.ps1
# Or with explicit directory and startup entry:
.\setup\Install.ps1 -InstallDir "C:\Tools\PowerProfile" -AddToStartup
```

### Uninstall

```powershell
.\setup\Install.ps1 -Uninstall
```

---

## Project layout

```
PowerProfile.App/
  Native/
    NativeMethods.cs       P/Invoke: EnumDisplaySettings, SystemParametersInfo,
                           GetSystemPowerStatus, SendMessageTimeout
  Services/
    DisplayService.cs      Refresh-rate detection and switching
    AnimationService.cs    Full animation enable/disable
    PowerStateService.cs   AC vs battery detection
    SettingsManager.cs     JSON settings load/save
  Models/
    AppSettings.cs         Serializable settings model
  UI/
    MainForm.cs            WinForms main window (two-tab layout)
  Program.cs
setup/
  PowerProfile.iss         Inno Setup 6 installer script
  Install.ps1              Zero-dependency PowerShell installer
build.ps1                  Build + optional installer compilation
NuGet.Config               Clears stale VS fallback package folder
```

---

## Legacy reference

The original PowerShell-based implementation is preserved under `backup/PowerChange/`.
The key behavioral difference: this version detects available refresh rates from the driver rather than hardcoding 60/90 Hz, and exposes all options in a UI rather than running silently as a scheduled task.
