namespace PowerProfile.App.Models;

/// <summary>State shared with the Flutter dashboard through the local IPC bridge.</summary>
public sealed record PowerProfileState(
    string PowerSource,
    uint CurrentRefreshRate,
    uint CurrentWidth,
    uint CurrentHeight,
    IReadOnlyList<uint> AvailableRefreshRates,
    bool AnimationsEnabled,
    AppSettings Settings);

/// <summary>Validated settings supplied by the Flutter dashboard.</summary>
public sealed record PowerProfileApplyRequest(
    bool RefreshRateEnabled,
    bool RefreshRateAutoSwitch,
    uint RefreshRateAC,
    uint RefreshRateBattery,
    bool AnimationsEnabled,
    bool AnimationsAutoSwitch,
    bool AnimationsOnAC,
    bool AnimationsOnBattery,
    AnimationPolicy? AnimationPolicyAC,
    AnimationPolicy? AnimationPolicyBattery,
    string? UiLanguage);
