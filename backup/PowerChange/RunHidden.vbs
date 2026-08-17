Set objShell = CreateObject("WScript.Shell")
objShell.Run "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File ""C:\Program Files\PowerChange\PowerProfileSwitch.ps1""", 0, False
