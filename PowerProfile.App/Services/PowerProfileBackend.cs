using System.Text.Json;
using PowerProfile.App.Models;

namespace PowerProfile.App.Services;

/// <summary>
/// Keeps Windows-specific operations behind a small, validated API used by IPC.
/// It intentionally shares the same persisted settings model as the native host.
/// </summary>
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
        return new PowerProfileState(
            PowerStateService.GetCurrentSource().ToString(),
            mode.RefreshRate,
            mode.Width,
            mode.Height,
            DisplayService.GetAvailableRefreshRates(),
            AnimationService.GetMinAnimateEnabled(),
            CloneSettings(_settings.Settings));
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
        _settings.Save();

        ApplyRefreshRateForCurrentPower(settings);
        ApplyAnimationsForCurrentPower(settings);
        return GetState();
    }

    public PowerProfileState ApplyRefreshRate(JsonElement payload)
    {
        var request = DeserializePayload<RefreshRateRequest>(payload);
        var rates = DisplayService.GetAvailableRefreshRates();
        if (!rates.Contains(request.RefreshRate))
            throw new BackendCommandException("invalid_refresh_rate", $"{request.RefreshRate} Hz is not available for the current display resolution.");
        if (!DisplayService.SetRefreshRate(request.RefreshRate))
            throw new BackendCommandException("refresh_rate_failed", $"Windows rejected {request.RefreshRate} Hz.");
        return GetState();
    }

    public PowerProfileState ApplyAnimations(JsonElement payload)
    {
        var request = DeserializePayload<AnimationsRequest>(payload);
        AnimationService.SetAnimations(request.Enabled);
        return GetState();
    }

    private static T DeserializePayload<T>(JsonElement payload)
    {
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new BackendCommandException("invalid_payload", "This command requires a JSON payload.");
        try
        {
            return payload.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new BackendCommandException("invalid_payload", "The request payload is empty.");
        }
        catch (JsonException ex)
        {
            throw new BackendCommandException("invalid_payload", ex.Message);
        }
    }

    private static void ValidateRates(uint acRate, uint batteryRate)
    {
        var rates = DisplayService.GetAvailableRefreshRates();
        foreach (var rate in new[] { acRate, batteryRate })
        {
            if (rate != 0 && !rates.Contains(rate))
                throw new BackendCommandException("invalid_refresh_rate", $"{rate} Hz is not available for the current display resolution.");
        }
    }

    private static void ApplyRefreshRateForCurrentPower(AppSettings settings)
    {
        if (!settings.RefreshRateEnabled)
            return;

        var rates = DisplayService.GetAvailableRefreshRates();
        if (rates.Count == 0)
            throw new BackendCommandException("no_refresh_rates", "No refresh rates are available for the current display.");

        var powerSource = PowerStateService.GetCurrentSource();
        var onBattery = powerSource == PowerStateService.PowerSource.Battery;
        var configuredRate = settings.RefreshRateAutoSwitch && onBattery
            ? settings.RefreshRateBattery
            : settings.RefreshRateAC;
        var targetRate = configuredRate == 0
            ? (onBattery ? rates[0] : rates[^1])
            : configuredRate;

        if (!DisplayService.SetRefreshRate(targetRate))
            throw new BackendCommandException("refresh_rate_failed", $"Windows rejected {targetRate} Hz.");
    }

    private static void ApplyAnimationsForCurrentPower(AppSettings settings)
    {
        if (!settings.AnimationsEnabled)
            return;

        var powerSource = PowerStateService.GetCurrentSource();
        var enabled = settings.AnimationsAutoSwitch
            ? (powerSource == PowerStateService.PowerSource.Battery ? settings.AnimationsOnBattery : settings.AnimationsOnAC)
            : settings.AnimationsOnAC;
        AnimationService.SetAnimations(enabled);
    }

    private static AppSettings CloneSettings(AppSettings settings) => new()
    {
        RefreshRateEnabled = settings.RefreshRateEnabled,
        RefreshRateAutoSwitch = settings.RefreshRateAutoSwitch,
        RefreshRateAC = settings.RefreshRateAC,
        RefreshRateBattery = settings.RefreshRateBattery,
        AnimationsEnabled = settings.AnimationsEnabled,
        AnimationsAutoSwitch = settings.AnimationsAutoSwitch,
        AnimationsOnAC = settings.AnimationsOnAC,
        AnimationsOnBattery = settings.AnimationsOnBattery,
    };

    private sealed record RefreshRateRequest(uint RefreshRate);
    private sealed record AnimationsRequest(bool Enabled);
}

public sealed class BackendCommandException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
