# PowerProfile

PowerProfile is a Windows utility for display refresh-rate and animation profiles. **The Flutter dashboard is now the primary user-facing UI**; the .NET app remains the native backend and notification-area host.

## Architecture

- **PowerProfile.FlutterUI** — primary dark, modern dashboard with sidebar navigation, overview, refresh-rate, animation, settings-style cards, and branded icon. On Windows it uses the native named-pipe service; non-Windows runs use an explicit in-memory preview service because the backend is Windows-only.
- **PowerProfile.App** — .NET 10 WinForms native host/backend. It owns the existing Windows display, animation, power-state, persistence, and tray functionality. It starts a local named-pipe server while alive, and its startup path resolves and launches the Flutter Windows executable in published layouts and source-checkout Debug/Release layouts.

## Local IPC protocol

The host exposes `\\.\pipe\PowerProfile.LocalApi.v1` only while the tray process is running. Each connection sends one UTF-8 JSON line and receives one UTF-8 JSON response line. Requests use `{ "id": "...", "version": 1, "command": "getState|apply|applyRefreshRate|applyAnimations", "payload": {} }`; responses use `{ "id": "...", "ok": true, "data": {} }` or `{ "id": "...", "ok": false, "error": { "code": "...", "message": "..." } }`. `getState` returns power source, current display mode, available rates, animation state, and persisted settings. `apply` validates and persists the full settings payload, then applies the current power profile. The narrower apply commands are used for focused operations. A missing pipe is reported as a connection error in the dashboard; unsupported platforms do not claim native integration.


From PowerShell in the repository root:

```powershell
dotnet build PowerProfile.App\PowerProfile.App.csproj
.\scripts\publish.ps1
```

The publish script produces a self-contained Windows build and, when Inno Setup is installed, `scripts\dist\PowerProfile-Setup-1.0.0.exe`. The native host's normal launch and tray **Open PowerProfile** action both resolve the Flutter dashboard. If the Flutter executable is missing, the native form remains available and shows an actionable warning.

## Build and verify the Flutter dashboard

Flutter is expected at `C:\ProgramData\flutter`; use `FLUTTER_EXE` to override it. The scripts use their own location and do not depend on the old checkout path:

```powershell
Set-Location .\PowerProfile.FlutterUI
.\verify-windows.ps1
.\verify-launch.ps1
```

`verify-windows.ps1` runs `pub get`, analyze, tests, and `flutter build windows --debug`. `verify-launch.ps1` checks the expected debug executable path, launches it, and closes it after the smoke check.

The supplied branding asset is copied from `assets\PowerProfile-icon.png` into the Flutter project. On Windows, Flutter communicates with the C# host through the named-pipe bridge described above; non-Windows runs use the explicit in-memory preview service.

## Tray behavior

Closing the native host hides it to the notification area; **one left click** on the tray icon launches/restores the Flutter dashboard. The context menu remains available for Open and Exit. There is no double-click tray subscription or code path.

The legacy PowerShell implementation remains under `backup/PowerChange/` for reference.
