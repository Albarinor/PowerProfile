using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PowerProfile.App.Models;
using PowerProfile.App.Native;

namespace PowerProfile.App.Services;

/// <summary>Reads and applies individually configurable Windows visual effects.</summary>
public static class AnimationService
{
    private const uint SPIF = NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE;
    private const string KeyExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string KeyWindowMetrics = @"Control Panel\Desktop\WindowMetrics";

    public static bool GetMinAnimateEnabled()
    {
        var info = new NativeMethods.ANIMATIONINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.ANIMATIONINFO>() };
        return NativeMethods.SystemParametersInfoAni(NativeMethods.SPI_GETANIMATION, info.cbSize, ref info, 0) && info.iMinAnimate != 0;
    }

    public static bool GetTaskbarAnimations()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyExplorerAdv);
        return key?.GetValue("TaskbarAnimations") is int value ? value != 0 : true;
    }

    /// <summary>Compatibility method that applies one state to the entire visual-effects suite.</summary>
    public static AnimationApplyResult SetAnimations(bool enabled) => Apply(AnimationPolicy.FromLegacy(enabled));

    /// <summary>
    /// Applies each policy to its corresponding Windows setting. Taskbar animations
    /// controls Explorer taskbar/thumbnail preview animation through TaskbarAnimations.
    /// </summary>
    public static AnimationApplyResult Apply(AnimationPolicy policy)
    {
        var failures = new List<string>();
        SetWindowAnimation(policy.WindowAnimation, failures);
        SetSpi(NativeMethods.SPI_SETLISTBOXSMOOTHSCROLLING, policy.ListBoxSmoothScrolling, "list-box smooth scrolling", failures);
        SetSpi(NativeMethods.SPI_SETMENUANIMATION, policy.MenuAnimation, "menu animation", failures);
        SetSpi(NativeMethods.SPI_SETCOMBOBOXANIMATION, policy.ComboBoxAnimation, "combo-box animation", failures);
        SetSpi(NativeMethods.SPI_SETSELECTIONFADE, policy.SelectionFade, "selection fade", failures);
        SetSpi(NativeMethods.SPI_SETTOOLTIPANIMATION, policy.TooltipAnimation, "tooltip animation", failures);
        SetSpi(NativeMethods.SPI_SETTOOLTIPFADE, policy.TooltipFade, "tooltip fade", failures);
        SetSpi(NativeMethods.SPI_SETCURSORSHADOW, policy.CursorShadow, "cursor shadow", failures);
        SetSpi(NativeMethods.SPI_SETUIEFFECTS, policy.UiEffects, "UI effects", failures);
        SetSpi(NativeMethods.SPI_SETCLIENTAREAANIMATION, policy.ClientAreaAnimation, "client-area animation", failures);
        SetSpi(NativeMethods.SPI_SETDISABLEOVERLAPPEDCONTENT, policy.DisableOverlappedContent, "disable overlapped content", failures);
        var taskbarChanged = GetTaskbarAnimations() != policy.TaskbarAnimations;
        SetRegistry(policy, failures);
        BroadcastSettingChange();
        if (taskbarChanged && !RestartExplorer())
            failures.Add("Explorer refresh for taskbar animation policy");
        return new AnimationApplyResult(failures);
    }

    private static bool RestartExplorer()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try { process.CloseMainWindow(); } catch { }
                try { if (!process.WaitForExit(1500)) process.Kill(); } catch { }
                process.Dispose();
            }
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SetWindowAnimation(bool enabled, ICollection<string> failures)
    {
        var info = new NativeMethods.ANIMATIONINFO
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.ANIMATIONINFO>(), iMinAnimate = enabled ? 1 : 0,
        };
        if (!NativeMethods.SystemParametersInfoAni(NativeMethods.SPI_SETANIMATION, info.cbSize, ref info, SPIF))
            failures.Add("minimize/maximize animation");
    }

    private static void SetSpi(uint action, bool enabled, string name, ICollection<string> failures)
    {
        if (!NativeMethods.SystemParametersInfoInt(action, 0, enabled ? 1 : 0, SPIF))
            failures.Add(name);
    }

    private static void SetRegistry(AnimationPolicy policy, ICollection<string> failures)
    {
        try
        {
            using var explorer = Registry.CurrentUser.CreateSubKey(KeyExplorerAdv);
            explorer?.SetValue("TaskbarAnimations", policy.TaskbarAnimations ? 1 : 0, RegistryValueKind.DWord);
            if (GetTaskbarAnimations() != policy.TaskbarAnimations)
                failures.Add("taskbar and thumbnail preview animations");
            using var metrics = Registry.CurrentUser.CreateSubKey(KeyWindowMetrics);
            metrics?.SetValue("MinAnimate", policy.WindowAnimation ? "1" : "0", RegistryValueKind.String);
        }
        catch (Exception)
        {
            failures.Add("taskbar animation registry settings");
        }
    }

    private static void BroadcastSettingChange()
    {
        NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
            UIntPtr.Zero, "Software", NativeMethods.SMTO_ABORTIFHUNG, 100, out _);
        NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
            UIntPtr.Zero, "Environment", NativeMethods.SMTO_ABORTIFHUNG, 100, out _);
    }
}

public sealed record AnimationApplyResult(IReadOnlyList<string> FailedSettings)
{
    public bool Succeeded => FailedSettings.Count == 0;
    public string ErrorMessage => $"Windows could not apply: {string.Join(", ", FailedSettings)}.";
}
