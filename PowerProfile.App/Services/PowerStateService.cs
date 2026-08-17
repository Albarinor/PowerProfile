using PowerProfile.App.Native;

namespace PowerProfile.App.Services;

/// <summary>Reads whether the machine is on AC or battery power.</summary>
public static class PowerStateService
{
    public enum PowerSource { AC, Battery, Unknown }

    public static PowerSource GetCurrentSource()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var s))
            return PowerSource.Unknown;

        return s.ACLineStatus switch
        {
            1   => PowerSource.AC,
            0   => PowerSource.Battery,
            _   => PowerSource.Unknown,
        };
    }

    public static bool IsOnAC()     => GetCurrentSource() == PowerSource.AC;
    public static bool IsOnBattery()=> GetCurrentSource() == PowerSource.Battery;
}
