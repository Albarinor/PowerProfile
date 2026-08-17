namespace PowerProfile.App.Models;

/// <summary>
/// Persisted application settings.  Serialized to/from JSON in %AppData%\PowerProfile.
/// </summary>
public sealed class AppSettings
{
    // ── Refresh-rate feature ─────────────────────────────────────────────────

    /// <summary>Master switch for the refresh-rate feature.</summary>
    public bool RefreshRateEnabled { get; set; } = true;

    /// <summary>Automatically switch based on AC/battery state.</summary>
    public bool RefreshRateAutoSwitch { get; set; } = true;

    /// <summary>Refresh rate (Hz) to use when on AC power.</summary>
    public uint RefreshRateAC { get; set; } = 0;       // 0 = "use highest available"

    /// <summary>Refresh rate (Hz) to use when on battery.</summary>
    public uint RefreshRateBattery { get; set; } = 0;  // 0 = "use lowest available"

    // ── Animation feature ────────────────────────────────────────────────────

    /// <summary>Master switch for the animation feature.</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>Automatically toggle animations based on AC/battery state.</summary>
    public bool AnimationsAutoSwitch { get; set; } = true;

    /// <summary>Enable animations when on AC power.</summary>
    public bool AnimationsOnAC { get; set; } = true;

    /// <summary>Enable animations when on battery.</summary>
    public bool AnimationsOnBattery { get; set; } = false;
}
