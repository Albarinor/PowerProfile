namespace PowerProfile.App.Models;

/// <summary>Independent Windows visual-effect values for one power source.</summary>
public sealed class AnimationPolicy
{
    public bool WindowAnimation { get; set; } = true;
    public bool TaskbarAnimations { get; set; } = true;
    public bool MenuAnimation { get; set; } = true;
    public bool ComboBoxAnimation { get; set; } = true;
    public bool ListBoxSmoothScrolling { get; set; } = true;
    public bool SelectionFade { get; set; } = true;
    public bool TooltipAnimation { get; set; } = true;
    public bool TooltipFade { get; set; } = true;
    public bool CursorShadow { get; set; } = true;
    public bool UiEffects { get; set; } = true;
    public bool ClientAreaAnimation { get; set; } = true;
    /// <summary>Windows SPI_DISABLEOVERLAPPEDCONTENT flag; true disables overlapped content.</summary>
    public bool DisableOverlappedContent { get; set; }

    public static AnimationPolicy FromLegacy(bool enabled) => new()
    {
        WindowAnimation = enabled, TaskbarAnimations = enabled, MenuAnimation = enabled,
        ComboBoxAnimation = enabled, ListBoxSmoothScrolling = enabled, SelectionFade = enabled,
        TooltipAnimation = enabled, TooltipFade = enabled, CursorShadow = enabled,
        UiEffects = enabled, ClientAreaAnimation = enabled, DisableOverlappedContent = !enabled,
    };

    public AnimationPolicy Clone() => new()
    {
        WindowAnimation = WindowAnimation, TaskbarAnimations = TaskbarAnimations, MenuAnimation = MenuAnimation,
        ComboBoxAnimation = ComboBoxAnimation, ListBoxSmoothScrolling = ListBoxSmoothScrolling,
        SelectionFade = SelectionFade, TooltipAnimation = TooltipAnimation, TooltipFade = TooltipFade,
        CursorShadow = CursorShadow, UiEffects = UiEffects, ClientAreaAnimation = ClientAreaAnimation,
        DisableOverlappedContent = DisableOverlappedContent,
    };
}

/// <summary>Persisted application settings. Serialized to/from JSON in %AppData%\PowerProfile.</summary>
public sealed class AppSettings
{
    public bool RefreshRateEnabled { get; set; } = true;
    public bool RefreshRateAutoSwitch { get; set; } = true;
    public uint RefreshRateAC { get; set; }
    public uint RefreshRateBattery { get; set; }

    // Kept for compatibility with existing settings.json and bridge clients.
    public bool AnimationsEnabled { get; set; } = true;
    public bool AnimationsAutoSwitch { get; set; } = true;
    public bool AnimationsOnAC { get; set; } = true;
    public bool AnimationsOnBattery { get; set; }
    public string UiLanguage { get; set; } = "en";

    // Nullable specifically so legacy JSON can be migrated after deserialization.
    public AnimationPolicy? AnimationPolicyAC { get; set; }
    public AnimationPolicy? AnimationPolicyBattery { get; set; }

    public void NormalizeAnimationPolicies()
    {
        AnimationPolicyAC ??= AnimationPolicy.FromLegacy(AnimationsOnAC);
        AnimationPolicyBattery ??= AnimationPolicy.FromLegacy(AnimationsOnBattery);
    }

    public AppSettings Clone()
    {
        NormalizeAnimationPolicies();
        return new AppSettings
        {
            RefreshRateEnabled = RefreshRateEnabled, RefreshRateAutoSwitch = RefreshRateAutoSwitch,
            RefreshRateAC = RefreshRateAC, RefreshRateBattery = RefreshRateBattery,
            AnimationsEnabled = AnimationsEnabled, AnimationsAutoSwitch = AnimationsAutoSwitch,
            AnimationsOnAC = AnimationsOnAC, AnimationsOnBattery = AnimationsOnBattery,
            UiLanguage = UiLanguage,
            AnimationPolicyAC = AnimationPolicyAC!.Clone(), AnimationPolicyBattery = AnimationPolicyBattery!.Clone(),
        };
    }
}
