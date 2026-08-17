using PowerProfile.App.Native;
using System.Runtime.InteropServices;

namespace PowerProfile.App.Services;

/// <summary>Wraps EnumDisplaySettings / ChangeDisplaySettingsEx.</summary>
public sealed class DisplayService
{
    /// <summary>One supported display mode discovered from the current adapter.</summary>
    public sealed record DisplayMode(uint Width, uint Height, uint RefreshRate)
    {
        public override string ToString() => $"{Width}x{Height} @ {RefreshRate} Hz";
    }

    // ── Query ────────────────────────────────────────────────────────────────

    /// <summary>Returns all unique refresh rates supported at the current resolution.</summary>
    public static List<uint> GetAvailableRefreshRates()
    {
        var current = GetCurrentMode();
        var rates   = new SortedSet<uint>();

        var dm = new NativeMethods.DEVMODE();
        dm.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();
        for (int i = 0; NativeMethods.EnumDisplaySettings(null, i, ref dm); i++)
        {
            if (dm.dmPelsWidth == current.Width && dm.dmPelsHeight == current.Height)
                rates.Add(dm.dmDisplayFrequency);
        }
        return [.. rates];
    }

    /// <summary>Returns ALL display modes (all resolutions + refresh rates).</summary>
    public static List<DisplayMode> GetAllModes()
    {
        var modes = new List<DisplayMode>();
        var dm    = new NativeMethods.DEVMODE();
        dm.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();
        for (int i = 0; NativeMethods.EnumDisplaySettings(null, i, ref dm); i++)
            modes.Add(new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency));
        return modes;
    }

    /// <summary>Returns the currently active display mode.</summary>
    public static DisplayMode GetCurrentMode()
    {
        var dm = new NativeMethods.DEVMODE();
        dm.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();
        NativeMethods.EnumDisplaySettings(null, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm);
        return new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency);
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Changes only the refresh rate while keeping the current resolution.
    /// Returns true on success.
    /// </summary>
    public static bool SetRefreshRate(uint hz)
    {
        var current = GetCurrentMode();
        var dm      = new NativeMethods.DEVMODE();
        dm.dmSize             = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();
        dm.dmFields           = NativeMethods.DM_PELSWIDTH
                              | NativeMethods.DM_PELSHEIGHT
                              | NativeMethods.DM_DISPLAYFREQUENCY;
        dm.dmPelsWidth        = current.Width;
        dm.dmPelsHeight       = current.Height;
        dm.dmDisplayFrequency = hz;

        int result = NativeMethods.ChangeDisplaySettingsEx(
            null, ref dm, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY, IntPtr.Zero);
        return result == NativeMethods.DISP_CHANGE_SUCCESSFUL;
    }
}
