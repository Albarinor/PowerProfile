using Microsoft.Win32;

namespace PowerProfile.App.Services;

/// <summary>Reapplies source-specific policies when Windows reports a power-status change.</summary>
public sealed class PowerSourceMonitor : IDisposable
{
    private readonly PowerProfileBackend _backend;
    private PowerStateService.PowerSource _lastSource;
    private bool _disposed;

    public PowerSourceMonitor(PowerProfileBackend backend)
    {
        _backend = backend;
        _lastSource = PowerStateService.GetCurrentSource();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (_disposed || e.Mode != PowerModes.StatusChange) return;
        var source = PowerStateService.GetCurrentSource();
        if (source == PowerStateService.PowerSource.Unknown || source == _lastSource) return;
        _lastSource = source;
        try { _backend.ApplyForCurrentPower(); }
        catch { /* The dashboard surfaces apply errors for explicit user actions. */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
