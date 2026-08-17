using System.Runtime.InteropServices;

namespace PowerProfile.App.Native;

/// <summary>P/Invoke declarations for display and animation Win32 APIs.</summary>
internal static class NativeMethods
{
    // ── Display ──────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint   dmFields;
        public int    dmPositionX;
        public int    dmPositionY;
        public uint   dmDisplayOrientation;
        public uint   dmDisplayFixedOutput;
        public short  dmColor;
        public short  dmDuplex;
        public short  dmYResolution;
        public short  dmTTOption;
        public short  dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint   dmBitsPerPel;
        public uint   dmPelsWidth;
        public uint   dmPelsHeight;
        public uint   dmDisplayFlags;
        public uint   dmDisplayFrequency;
        public uint   dmICMMethod;
        public uint   dmICMIntent;
        public uint   dmMediaType;
        public uint   dmDitherType;
        public uint   dmReserved1;
        public uint   dmReserved2;
        public uint   dmPanningWidth;
        public uint   dmPanningHeight;
    }

    public const uint DM_DISPLAYFREQUENCY = 0x00400000;
    public const uint DM_PELSWIDTH        = 0x00080000;
    public const uint DM_PELSHEIGHT       = 0x00100000;
    public const int  ENUM_CURRENT_SETTINGS = -1;
    public const int  CDS_UPDATEREGISTRY    = 0x01;
    public const int  CDS_TEST              = 0x02;
    public const int  DISP_CHANGE_SUCCESSFUL = 0;

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplaySettings(
        string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName, ref DEVMODE lpDevMode,
        IntPtr hwnd, uint dwflags, IntPtr lParam);

    // ── Animation ────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct ANIMATIONINFO
    {
        public uint cbSize;
        public int  iMinAnimate;
    }

    public const uint SPI_GETANIMATION  = 0x0048;
    public const uint SPI_SETANIMATION  = 0x0049;
    public const uint SPIF_UPDATEINIFILE = 0x0001;
    public const uint SPIF_SENDCHANGE    = 0x0002;

    // Additional SPI codes used by the legacy script for full animation coverage
    public static readonly uint[] SPI_ANIMATION_EXTRAS =
    [
        0x1004, // SPI_SETLISTBOXSMOOTHSCROLLING
        0x1006, // SPI_SETMENUANIMATION
        0x1002, // SPI_SETCOMBOBOXANIMATION
        0x1014, // SPI_SETTOOLTIPANIMATION
        0x1016, // SPI_SETTOOLTIPFADE
        0x1018, // SPI_SETCURSORSHADOW
        0x101A, // SPI_SETUIEFFECTS
        0x103F, // SPI_SETCLIENTAREAANIMATION
        0x1042, // SPI_SETDISABLEOVERLAPPEDCONTENT
    ];

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SystemParametersInfoW")]
    public static extern bool SystemParametersInfoAni(
        uint uiAction, uint uiParam, ref ANIMATIONINFO pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SystemParametersInfoW")]
    public static extern bool SystemParametersInfoInt(
        uint uiAction, uint uiParam, int pvParam, uint fWinIni);

    // ── WM_SETTINGCHANGE broadcast ────────────────────────────────────────────

    public const IntPtr HWND_BROADCAST = unchecked((IntPtr)0xFFFF);
    public const uint   WM_SETTINGCHANGE = 0x001A;
    public const uint   SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, UIntPtr wParam,
        string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    // ── Power state ──────────────────────────────────────────────────────────

    [DllImport("kernel32.dll")]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte  ACLineStatus;        // 0=offline(battery), 1=online(AC), 255=unknown
        public byte  BatteryFlag;
        public byte  BatteryLifePercent;
        public byte  SystemStatusFlag;
        public uint  BatteryLifeTime;
        public uint  BatteryFullLifeTime;
    }
}
