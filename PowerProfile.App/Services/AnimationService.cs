using Microsoft.Win32;
using System.Runtime.InteropServices;
using PowerProfile.App.Native;

namespace PowerProfile.App.Services;

/// <summary>
/// Reads and writes Windows animation settings via SystemParametersInfo
/// and the corresponding registry keys used by Explorer/DWM.
/// </summary>
public sealed class AnimationService
{
    private const uint SPIF = NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE;

    // ── Registry paths ───────────────────────────────────────────────────────

    private const string KeyExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string KeyWindowMetrics = @"Control Panel\Desktop\WindowMetrics";
    private const string KeyVisualEffects = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";

    // ── Read current state ───────────────────────────────────────────────────

    /// <summary>Returns true if the primary MinAnimate animation is currently on.</summary>
    public static bool GetMinAnimateEnabled()
    {
        var info = new NativeMethods.ANIMATIONINFO();
        info.cbSize = (uint)Marshal.SizeOf<NativeMethods.ANIMATIONINFO>();
        NativeMethods.SystemParametersInfoAni(
            NativeMethods.SPI_GETANIMATION, info.cbSize, ref info, 0);
        return info.iMinAnimate != 0;
    }

    /// <summary>Reads TaskbarAnimations registry value (1=on, 0=off).</summary>
    public static bool GetTaskbarAnimations()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyExplorerAdv);
        return key?.GetValue("TaskbarAnimations") is int v ? v != 0 : true;
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enables or disables the full suite of Windows animations:
    ///   - MinAnimate (minimize/maximize)
    ///   - Taskbar animations
    ///   - Window metrics MinAnimate flag
    ///   - Extra SPI animation codes from the legacy script
    /// Broadcasts WM_SETTINGCHANGE so Explorer picks up the change immediately.
    /// </summary>
    public static void SetAnimations(bool enable)
    {
        int val = enable ? 1 : 0;

        // 1. SPI_SETANIMATION (minimize/maximize animations)
        var info = new NativeMethods.ANIMATIONINFO();
        info.cbSize     = (uint)Marshal.SizeOf<NativeMethods.ANIMATIONINFO>();
        info.iMinAnimate = val;
        NativeMethods.SystemParametersInfoAni(
            NativeMethods.SPI_SETANIMATION, info.cbSize, ref info, SPIF);

        // 2. Extra SPI codes (menu animation, combo, tooltip, UI effects, etc.)
        foreach (uint spi in NativeMethods.SPI_ANIMATION_EXTRAS)
            NativeMethods.SystemParametersInfoInt(spi, 0, val, SPIF);

        // 3. Registry: TaskbarAnimations
        using (var key = Registry.CurrentUser.CreateSubKey(KeyExplorerAdv))
            key?.SetValue("TaskbarAnimations", val, RegistryValueKind.DWord);

        // 4. Registry: WindowMetrics MinAnimate ("1"/"0" as string)
        using (var key = Registry.CurrentUser.CreateSubKey(KeyWindowMetrics))
            key?.SetValue("MinAnimate", val.ToString(), RegistryValueKind.String);

        // 5. Broadcast so running Explorer / shell reflects the change
        BroadcastSettingChange();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void BroadcastSettingChange()
    {
        UIntPtr result = UIntPtr.Zero;
        NativeMethods.SendMessageTimeout(
            NativeMethods.HWND_BROADCAST,
            NativeMethods.WM_SETTINGCHANGE,
            UIntPtr.Zero,
            "Software",
            NativeMethods.SMTO_ABORTIFHUNG,
            100,
            out result);
    }
}
