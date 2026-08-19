using PowerProfile.App.Services;
using PowerProfile.App.UI;

namespace PowerProfile.App;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var backend = new PowerProfileBackend(new SettingsManager());
        var pipeServer = new PowerProfilePipeServer(backend);
        pipeServer.Start();

        try
        {
            using var form = new MainForm();
            form.Shown += (_, _) =>
            {
                if (form.LaunchPrimaryUi())
                {
                    // WinForms remains the native tray/backend host; Flutter is the
                    // only primary UI shown to the user.
                    form.Hide();
                }
                else
                {
                    form.ShowInTaskbar = true;
                }
            };
            form.ShowInTaskbar = false;
            Application.Run(form);
        }
        finally
        {
            pipeServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
