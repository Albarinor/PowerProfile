using System.Diagnostics;

namespace PowerProfile.App.Services;

/// <summary>
/// Locates and starts the Flutter Windows dashboard in development and published layouts.
/// </summary>
internal static class FlutterUiLauncher
{
    internal const string ExecutableName = "powerprofile_flutter_ui.exe";

    internal static string? ResolveExecutable(string? baseDirectory = null)
    {
        var basePath = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        var candidates = new List<string>
        {
            Path.Combine(basePath, ExecutableName),
            Path.Combine(basePath, "PowerProfile.FlutterUI", ExecutableName),
        };

        // In a source checkout AppContext.BaseDirectory is below the repository;
        // in a published build the dashboard is copied next to PowerProfile.exe.
        for (var directory = new DirectoryInfo(basePath);
             directory is not null;
             directory = directory.Parent)
        {
            candidates.Add(Path.Combine(
                directory.FullName,
                "PowerProfile.FlutterUI",
                "build",
                "windows",
                "x64",
                "runner",
                "Release",
                ExecutableName));
            candidates.Add(Path.Combine(
                directory.FullName,
                "PowerProfile.FlutterUI",
                "build",
                "windows",
                "x64",
                "runner",
                "Debug",
                ExecutableName));
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
    }

    internal static bool Launch()
    {
        var executable = ResolveExecutable();
        if (executable is null)
            return false;

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = true,
        });
        return true;
    }
}
