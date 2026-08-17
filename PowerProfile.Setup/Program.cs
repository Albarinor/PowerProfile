using System.Diagnostics;

namespace PowerProfile.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 PowerProfile 安装目录",
            SelectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerProfile")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var target = dialog.SelectedPath;
        var source = Path.Combine(AppContext.BaseDirectory, "PowerProfile.App");
        if (!Directory.Exists(source))
        {
            MessageBox.Show("安装包缺少 PowerProfile.App 目录。请保持 Setup.exe 与该目录在一起。", "PowerProfile Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        CopyDirectory(source, target);
        var exe = Path.Combine(target, "PowerProfile.exe");
        if (!File.Exists(exe))
        {
            MessageBox.Show("安装完成但未找到主程序。", "PowerProfile Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
}
