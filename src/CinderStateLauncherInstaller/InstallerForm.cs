using System.Diagnostics;
using System.Reflection;

namespace CinderStateLauncherInstaller;

internal sealed class InstallerForm : Form
{
    private const string LauncherExeName = "CinderStateLauncher.exe";
    private const string InstallFolderName = "Cinder State Launcher";
    private const string ShortcutName = "Cinder State Launcher.lnk";

    private readonly Label _statusLabel = new();
    private readonly Button _installButton = new();
    private readonly Button _launchButton = new();
    private readonly Button _closeButton = new();

    private string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private string InstallPath => Path.Combine(DesktopPath, InstallFolderName);
    private string LauncherPath => Path.Combine(InstallPath, LauncherExeName);
    private string ShortcutPath => Path.Combine(DesktopPath, ShortcutName);

    public InstallerForm()
    {
        BuildUi();
        ApplyWindowIcon();
    }

    private void BuildUi()
    {
        Text = "Cinder State Launcher Installer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 210);
        Font = new Font("Segoe UI", 9F);

        var titleLabel = new Label
        {
            Text = "Install Cinder State Launcher",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        var installPathLabel = new Label
        {
            Text = $"Install location: Desktop\\{InstallFolderName}",
            Location = new Point(22, 64),
            Size = new Size(410, 24)
        };

        _statusLabel.Text = "Ready to install.";
        _statusLabel.Location = new Point(22, 98);
        _statusLabel.Size = new Size(410, 40);

        _installButton.Text = "Install";
        _installButton.Location = new Point(160, 158);
        _installButton.Size = new Size(88, 30);
        _installButton.Click += (_, _) => InstallLauncher();

        _launchButton.Text = "Launch";
        _launchButton.Location = new Point(254, 158);
        _launchButton.Size = new Size(88, 30);
        _launchButton.Enabled = File.Exists(LauncherPath);
        _launchButton.Click += (_, _) => LaunchInstalledLauncher();

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(348, 158);
        _closeButton.Size = new Size(88, 30);
        _closeButton.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            titleLabel,
            installPathLabel,
            _statusLabel,
            _installButton,
            _launchButton,
            _closeButton
        });
    }

    private void ApplyWindowIcon()
    {
        Icon? appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (appIcon is not null)
        {
            Icon = appIcon;
        }
    }

    private void InstallLauncher()
    {
        SetBusy("Installing launcher...");

        try
        {
            Directory.CreateDirectory(InstallPath);
            ExtractLauncher();
            CreateDesktopShortcut();

            _statusLabel.Text = $"Installed. Use the Desktop shortcut: {Path.GetFileNameWithoutExtension(ShortcutName)}";
            _launchButton.Enabled = true;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Install failed.";
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _installButton.Enabled = true;
            _closeButton.Enabled = true;
        }
    }

    private void ExtractLauncher()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(".payload.CinderStateLauncher.exe", StringComparison.OrdinalIgnoreCase)
                                    || name.EndsWith(".CinderStateLauncher.exe", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException("The installer payload is missing.");
        }

        string tempPath = Path.Combine(InstallPath, $"{LauncherExeName}.tmp");
        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null)
        {
            throw new InvalidOperationException("The installer payload could not be opened.");
        }

        using (FileStream output = File.Create(tempPath))
        {
            resourceStream.CopyTo(output);
        }

        if (File.Exists(LauncherPath))
        {
            File.Delete(LauncherPath);
        }

        File.Move(tempPath, LauncherPath);
    }

    private void CreateDesktopShortcut()
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            CreateUrlShortcut();
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(ShortcutPath);
        shortcut.TargetPath = LauncherPath;
        shortcut.WorkingDirectory = InstallPath;
        shortcut.Description = "Cinder State Test Launcher";
        shortcut.IconLocation = LauncherPath;
        shortcut.Save();
    }

    private void CreateUrlShortcut()
    {
        string urlShortcutPath = Path.Combine(DesktopPath, "Cinder State Launcher.url");
        File.WriteAllText(urlShortcutPath, $"[InternetShortcut]{Environment.NewLine}URL=file:///{LauncherPath.Replace('\\', '/')}{Environment.NewLine}IconFile={LauncherPath}{Environment.NewLine}IconIndex=0{Environment.NewLine}");
    }

    private void LaunchInstalledLauncher()
    {
        try
        {
            if (!File.Exists(LauncherPath))
            {
                MessageBox.Show(this, "The launcher is not installed yet.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherPath,
                WorkingDirectory = InstallPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetBusy(string status)
    {
        _statusLabel.Text = status;
        _installButton.Enabled = false;
        _launchButton.Enabled = false;
        _closeButton.Enabled = false;
        Refresh();
    }
}
