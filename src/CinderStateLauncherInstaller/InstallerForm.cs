using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CinderStateLauncherInstaller;

internal sealed class InstallerForm : Form
{
    private const string LauncherExeName = "CinderStateLauncher.exe";
    private const string InstallFolderName = "Cinder State Launcher";
    private const string GameInstallFolderName = "Cinder State Test";
    private const string StartMenuFolderName = "Cinder State";
    private const string ShortcutName = "Cinder State Launcher.lnk";
    private const string UninstallScriptName = "Uninstall Cinder State Launcher.bat";
    private const string UninstallShortcutName = "Uninstall Cinder State Launcher.lnk";

    private readonly Label _statusLabel = new();
    private readonly Button _installButton = new();
    private readonly Button _launchButton = new();
    private readonly Button _closeButton = new();

    private string LocalAppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private string ProgramsPath => Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    private string InstallPath => Path.Combine(LocalAppDataPath, InstallFolderName);
    private string LauncherPath => Path.Combine(InstallPath, LauncherExeName);
    private string UninstallScriptPath => Path.Combine(InstallPath, UninstallScriptName);
    private string DesktopShortcutPath => Path.Combine(DesktopPath, ShortcutName);
    private string StartMenuFolderPath => Path.Combine(ProgramsPath, StartMenuFolderName);
    private string StartMenuShortcutPath => Path.Combine(StartMenuFolderPath, ShortcutName);
    private string StartMenuUninstallShortcutPath => Path.Combine(StartMenuFolderPath, UninstallShortcutName);

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
            Text = $"Launcher: %LOCALAPPDATA%\\{InstallFolderName}",
            Location = new Point(22, 64),
            Size = new Size(410, 24)
        };

        var gamePathLabel = new Label
        {
            Text = $"Game files: %LOCALAPPDATA%\\{GameInstallFolderName}",
            Location = new Point(22, 88),
            Size = new Size(410, 24)
        };

        _statusLabel.Text = "Ready to install.";
        _statusLabel.Location = new Point(22, 120);
        _statusLabel.Size = new Size(410, 40);

        _installButton.Text = "Install";
        _installButton.Location = new Point(160, 166);
        _installButton.Size = new Size(88, 30);
        _installButton.Click += (_, _) => InstallLauncher();

        _launchButton.Text = "Launch";
        _launchButton.Location = new Point(254, 166);
        _launchButton.Size = new Size(88, 30);
        _launchButton.Enabled = File.Exists(LauncherPath);
        _launchButton.Click += (_, _) => LaunchInstalledLauncher();

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(348, 166);
        _closeButton.Size = new Size(88, 30);
        _closeButton.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            titleLabel,
            installPathLabel,
            gamePathLabel,
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
            WriteUninstallScript();
            CreateShortcuts();

            _statusLabel.Text = "Installed. Use the Desktop or Start Menu shortcut.";
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

    private void WriteUninstallScript()
    {
        string[] lines =
        {
            "@echo off",
            "setlocal EnableExtensions",
            "",
            "set \"INSTALL_DIR=%LOCALAPPDATA%\\Cinder State Launcher\"",
            "set \"GAME_DIR=%LOCALAPPDATA%\\Cinder State Test\"",
            "set \"DESKTOP_SHORTCUT=%USERPROFILE%\\Desktop\\Cinder State Launcher.lnk\"",
            "set \"DESKTOP_URL=%USERPROFILE%\\Desktop\\Cinder State Launcher.url\"",
            "set \"START_MENU_DIR=%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Cinder State\"",
            "",
            "echo This will remove Cinder State Launcher and downloaded Cinder State Test files.",
            "choice /C YN /M \"Continue\"",
            "if errorlevel 2 exit /b 0",
            "",
            "taskkill /IM CinderStateLauncher.exe /F >nul 2>nul",
            "del /F /Q \"%DESKTOP_SHORTCUT%\" >nul 2>nul",
            "del /F /Q \"%DESKTOP_URL%\" >nul 2>nul",
            "rmdir /S /Q \"%START_MENU_DIR%\" >nul 2>nul",
            "rmdir /S /Q \"%GAME_DIR%\" >nul 2>nul",
            "cd /d \"%TEMP%\"",
            "start \"\" /min cmd /c \"timeout /t 2 /nobreak >nul & rmdir /s /q \"\"%INSTALL_DIR%\"\"\"",
            "echo Uninstall scheduled.",
            "exit /b 0"
        };

        File.WriteAllLines(UninstallScriptPath, lines, Encoding.ASCII);
    }

    private void CreateShortcuts()
    {
        Directory.CreateDirectory(StartMenuFolderPath);
        CreateShortcut(DesktopShortcutPath, LauncherPath, InstallPath, "Cinder State Test Launcher");
        CreateShortcut(StartMenuShortcutPath, LauncherPath, InstallPath, "Cinder State Test Launcher");
        CreateShortcut(StartMenuUninstallShortcutPath, UninstallScriptPath, InstallPath, "Uninstall Cinder State Launcher");
    }

    private void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            CreateUrlShortcut(shortcutPath, targetPath);
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = description;
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private void CreateUrlShortcut(string shortcutPath, string targetPath)
    {
        string urlShortcutPath = Path.ChangeExtension(shortcutPath, ".url");
        File.WriteAllText(urlShortcutPath, $"[InternetShortcut]{Environment.NewLine}URL=file:///{targetPath.Replace('\\', '/')}{Environment.NewLine}IconFile={targetPath}{Environment.NewLine}IconIndex=0{Environment.NewLine}");
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
