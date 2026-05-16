namespace CinderStateLauncher;

internal sealed class LauncherForm : Form
{
    private readonly LauncherService _launcherService;
    private readonly Label _statusLabel = new();
    private readonly Label _currentVersionLabel = new();
    private readonly Label _latestVersionLabel = new();
    private readonly Label _notesLabel = new();
    private readonly Button _primaryButton = new();
    private readonly Button _refreshButton = new();
    private readonly ProgressBar _progressBar = new();
    private LauncherState? _state;
    private CancellationTokenSource? _operationCts;

    public LauncherForm(LauncherService launcherService)
    {
        _launcherService = launcherService;
        BuildUi();
        ApplyWindowIcon();
        Shown += async (_, _) => await RefreshStateAsync();
        FormClosing += (_, _) => _operationCts?.Cancel();
    }

    private void BuildUi()
    {
        Text = "Cinder State Test Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ClientSize = new Size(460, 260);
        Font = new Font("Segoe UI", 9F);

        var titleLabel = new Label
        {
            Text = "Cinder State Test",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(20, 18),
            AutoSize = true
        };

        _statusLabel.Location = new Point(22, 60);
        _statusLabel.Size = new Size(410, 24);
        _statusLabel.Text = "Checking for updates...";

        _currentVersionLabel.Location = new Point(22, 96);
        _currentVersionLabel.Size = new Size(410, 24);
        _currentVersionLabel.Text = "Current version: Unknown";

        _latestVersionLabel.Location = new Point(22, 124);
        _latestVersionLabel.Size = new Size(410, 24);
        _latestVersionLabel.Text = "Latest version: Unknown";

        _notesLabel.Location = new Point(22, 152);
        _notesLabel.Size = new Size(410, 36);
        _notesLabel.Text = "";

        _progressBar.Location = new Point(22, 196);
        _progressBar.Size = new Size(410, 18);
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;

        _primaryButton.Location = new Point(276, 224);
        _primaryButton.Size = new Size(156, 30);
        _primaryButton.Text = "Checking...";
        _primaryButton.Enabled = false;
        _primaryButton.Click += async (_, _) => await RunPrimaryActionAsync();

        _refreshButton.Location = new Point(174, 224);
        _refreshButton.Size = new Size(96, 30);
        _refreshButton.Text = "Check";
        _refreshButton.Enabled = false;
        _refreshButton.Click += async (_, _) => await RefreshStateAsync();

        Controls.AddRange(new Control[]
        {
            titleLabel,
            _statusLabel,
            _currentVersionLabel,
            _latestVersionLabel,
            _notesLabel,
            _progressBar,
            _primaryButton,
            _refreshButton
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

    private async Task RefreshStateAsync()
    {
        SetBusy("Checking for updates...", 0);

        try
        {
            _operationCts?.Cancel();
            _operationCts = new CancellationTokenSource();
            _state = await _launcherService.CheckAsync(_operationCts.Token);
            RenderState(_state);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Update check failed: {ex.Message}";
            _latestVersionLabel.Text = "Latest version: Unavailable";

            if (_state?.HasInstalledBuild == true)
            {
                _primaryButton.Text = "Play Installed";
                _primaryButton.Enabled = true;
            }
            else
            {
                _primaryButton.Text = "Unavailable";
                _primaryButton.Enabled = false;
            }

            _refreshButton.Enabled = true;
        }
    }

    private void RenderState(LauncherState state)
    {
        _progressBar.Value = 0;
        _currentVersionLabel.Text = $"Current version: {(state.Local?.Version ?? "Not installed")}";
        _latestVersionLabel.Text = $"Latest version: {state.Remote?.Version ?? "Unknown"}";
        _notesLabel.Text = state.Remote?.Notes ?? "";

        if (!state.HasInstalledBuild)
        {
            _statusLabel.Text = "Ready to install.";
            _primaryButton.Text = "Install and Play";
        }
        else if (state.NeedsUpdate)
        {
            _statusLabel.Text = "Update available.";
            _primaryButton.Text = "Update and Play";
        }
        else
        {
            _statusLabel.Text = "Ready to play.";
            _primaryButton.Text = "Play";
        }

        _primaryButton.Enabled = true;
        _refreshButton.Enabled = true;
    }

    private async Task RunPrimaryActionAsync()
    {
        if (_state is null)
        {
            await RefreshStateAsync();
        }

        if (_state is null)
        {
            return;
        }

        try
        {
            _operationCts?.Cancel();
            _operationCts = new CancellationTokenSource();

            if (_state.NeedsUpdate)
            {
                if (_state.Remote is null)
                {
                    throw new InvalidOperationException("No remote version info is available.");
                }

                SetBusy("Preparing update...", 0);
                var status = new Progress<string>(text => _statusLabel.Text = text);
                var progress = new Progress<int>(value => _progressBar.Value = Math.Clamp(value, 0, 100));
                await _launcherService.UpdateAsync(_state.Remote, status, progress, _operationCts.Token);
                _state = await _launcherService.CheckAsync(_operationCts.Token);
                RenderState(_state);
            }

            _statusLabel.Text = "Launching Cinder State...";
            await _launcherService.PlayAsync(_state, _operationCts.Token);
            _statusLabel.Text = "Game launched.";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Operation failed.";
            MessageBox.Show(this, ex.Message, "Cinder State Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _refreshButton.Enabled = true;
            _primaryButton.Enabled = _state is not null;
        }
    }

    private void SetBusy(string status, int progress)
    {
        _statusLabel.Text = status;
        _progressBar.Value = Math.Clamp(progress, 0, 100);
        _primaryButton.Enabled = false;
        _refreshButton.Enabled = false;
    }
}
