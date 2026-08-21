using System.Text.Json;
using PowerProfile.App.Models;

namespace PowerProfile.App.Services;

/// <summary>Keeps Windows-specific operations behind the validated IPC API.</summary>
public sealed class PowerProfileBackend
{
    private readonly SettingsManager _settings;

    public PowerProfileBackend(SettingsManager settings)
    {
        _settings = settings;
        _settings.Load();
    }

    public PowerProfileState GetState()
    {
        var mode = DisplayService.GetCurrentMode();
        return new PowerProfileState(PowerStateService.GetCurrentSource().ToString(), mode.RefreshRate, mode.Width,
            mode.Height, DisplayService.GetAvailableRefreshRates(), AnimationService.GetMinAnimateEnabled(), _settings.Settings.Clone());
    }

    public PowerProfileState Apply(JsonElement payload)
    {
        var request = DeserializePayload<PowerProfileApplyRequest>(payload);
        ValidateRates(request.RefreshRateAC, request.RefreshRateBattery);
        var settings = _settings.Settings;
        settings.RefreshRateEnabled = request.RefreshRateEnabled;
        settings.RefreshRateAutoSwitch = request.RefreshRateAutoSwitch;
        settings.RefreshRateAC = request.RefreshRateAC;
        settings.RefreshRateBattery = request.RefreshRateBattery;
        settings.AnimationsEnabled = request.AnimationsEnabled;
        settings.AnimationsAutoSwitch = request.AnimationsAutoSwitch;
        settings.AnimationsOnAC = request.AnimationsOnAC;
        settings.AnimationsOnBattery = request.AnimationsOnBattery;
        if (request.UiLanguage is "en" or "zh") settings.UiLanguage = request.UiLanguage;
        settings.AnimationPolicyAC = request.AnimationPolicyAC?.Clone() ?? AnimationPolicy.FromLegacy(request.AnimationsOnAC);
        settings.AnimationPolicyBattery = request.AnimationPolicyBattery?.Clone() ?? AnimationPolicy.FromLegacy(request.AnimationsOnBattery);
        _settings.Save();
        ApplyForCurrentPower();
        return GetState();
    }

    /// <summary>Applies the saved source-specific profile after a power-status change.</summary>
    public void ApplyForCurrentPower()
    {
        var settings = _settings.Settings;
        ApplyRefreshRateForCurrentPower(settings);
        ApplyAnimationsForCurrentPower(settings);
    }

    /// <summary>Persists the dashboard language without replacing unsaved policy edits.</summary>
    public PowerProfileState SetLanguage(JsonElement payload)
    {
        var request = DeserializePayload<LanguageRequest>(payload);
        if (request.UiLanguage is not ("en" or "zh"))
            throw new BackendCommandException("invalid_language", "Language must be 'en' or 'zh'.");
        _settings.Settings.UiLanguage = request.UiLanguage;
        _settings.Save();
        return GetState();
    }

    public PowerProfileState ApplyRefreshRate(JsonElement payload)
    {
        var request = DeserializePayload<RefreshRateRequest>(payload);
        if (!DisplayService.GetAvailableRefreshRates().Contains(request.RefreshRate))
            throw new BackendCommandException("invalid_refresh_rate", $"{request.RefreshRate} Hz is not available for the current display resolution.");
        if (!DisplayService.SetRefreshRate(request.RefreshRate))
            throw new BackendCommandException("refresh_rate_failed", $"Windows rejected {request.RefreshRate} Hz.");
        return GetState();
    }

    public PowerProfileState ApplyAnimations(JsonElement payload)
    {
        var request = DeserializePayload<AnimationsRequest>(payload);
        var result = AnimationService.SetAnimations(request.Enabled);
        if (!result.Succeeded) throw new BackendCommandException("animation_apply_failed", result.ErrorMessage);
        return GetState();
    }

    private static T DeserializePayload<T>(JsonElement payload)
    {
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new BackendCommandException("invalid_payload", "This command requires a JSON payload.");
        try { return payload.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new BackendCommandException("invalid_payload", "The request payload is empty."); }
        catch (JsonException ex) { throw new BackendCommandException("invalid_payload", ex.Message); }
    }

    private static void ValidateRates(uint acRate, uint batteryRate)
    {
        var rates = DisplayService.GetAvailableRefreshRates();
        foreach (var rate in new[] { acRate, batteryRate })
            if (rate != 0 && !rates.Contains(rate))
                throw new BackendCommandException("invalid_refresh_rate", $"{rate} Hz is not available for the current display resolution.");
    }

    private static void ApplyRefreshRateForCurrentPower(AppSettings settings)
    {
        if (!settings.RefreshRateEnabled) return;
        var rates = DisplayService.GetAvailableRefreshRates();
        if (rates.Count == 0) throw new BackendCommandException("no_refresh_rates", "No refresh rates are available for the current display.");
        var source = PowerStateService.GetCurrentSource();
        if (source == PowerStateService.PowerSource.Unknown) return;
        var onBattery = source == PowerStateService.PowerSource.Battery;
        var configured = settings.RefreshRateAutoSwitch && onBattery ? settings.RefreshRateBattery : settings.RefreshRateAC;
        var target = configured == 0 ? (onBattery ? rates[0] : rates[^1]) : configured;
        if (!DisplayService.SetRefreshRate(target)) throw new BackendCommandException("refresh_rate_failed", $"Windows rejected {target} Hz.");
    }

    private static void ApplyAnimationsForCurrentPower(AppSettings settings)
    {
        var source = PowerStateService.GetCurrentSource();
        if (source == PowerStateService.PowerSource.Unknown) return;
        settings.NormalizeAnimationPolicies();
        var policy = !settings.AnimationsEnabled
            ? AnimationPolicy.FromLegacy(false)
            : settings.AnimationsAutoSwitch && source == PowerStateService.PowerSource.Battery
                ? settings.AnimationPolicyBattery!
                : settings.AnimationPolicyAC!;
        var result = AnimationService.Apply(policy);
        if (!result.Succeeded) throw new BackendCommandException("animation_apply_failed", result.ErrorMessage);
    }

    private sealed record RefreshRateRequest(uint RefreshRate);
    private sealed record LanguageRequest(string? UiLanguage);
    private sealed record AnimationsRequest(bool Enabled);
}

public sealed class BackendCommandException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
