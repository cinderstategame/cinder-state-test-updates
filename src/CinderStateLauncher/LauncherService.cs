using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace CinderStateLauncher;

internal sealed class LauncherService
{
    private const string VersionInfoUrl = "https://raw.githubusercontent.com/cinderstategame/cinder-state-test-updates/main/version.json";
    private const string InstallFolderName = "Cinder State Test";
    private const string MetadataFolderName = ".launcher";
    private const string LocalVersionFileName = "installed-version.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(30) };

    public string InstallRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        InstallFolderName);

    private string MetadataRoot => Path.Combine(InstallRoot, MetadataFolderName);
    private string LocalVersionPath => Path.Combine(MetadataRoot, LocalVersionFileName);

    public async Task<LauncherState> CheckAsync(CancellationToken cancellationToken)
    {
        LocalVersionInfo? local = await ReadLocalVersionAsync(cancellationToken);
        RemoteVersionInfo remote = await FetchRemoteVersionAsync(cancellationToken);
        bool hasInstalledBuild = local is not null && File.Exists(ResolveInstallPath(local.ExePath));
        bool needsUpdate = !hasInstalledBuild || IsRemoteNewer(local?.Version, remote.Version);

        return new LauncherState
        {
            Local = local,
            Remote = remote,
            HasInstalledBuild = hasInstalledBuild,
            NeedsUpdate = needsUpdate
        };
    }

    public async Task UpdateAsync(RemoteVersionInfo remote, IProgress<string> status, IProgress<int> progress, CancellationToken cancellationToken)
    {
        ValidateRemote(remote);

        string tempRoot = Path.Combine(Path.GetTempPath(), "CinderStateLauncher", Guid.NewGuid().ToString("N"));
        string zipPath = Path.Combine(tempRoot, "client.zip");
        string extractRoot = Path.Combine(tempRoot, "extract");
        string backupRoot = Path.Combine(tempRoot, "previous-install");

        Directory.CreateDirectory(tempRoot);

        try
        {
            status.Report("Downloading package...");
            await DownloadFileAsync(remote.DownloadUrl, zipPath, progress, cancellationToken);

            if (!string.IsNullOrWhiteSpace(remote.Sha256))
            {
                status.Report("Verifying package...");
                await VerifySha256Async(zipPath, remote.Sha256, cancellationToken);
            }

            status.Report("Extracting package...");
            ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true);

            string stagedExePath = ResolveStagedPath(extractRoot, remote.ExePath);
            if (!File.Exists(stagedExePath))
            {
                throw new InvalidOperationException($"The ZIP did not contain the expected executable: {remote.ExePath}");
            }

            status.Report("Installing package...");
            ReplaceInstallFolder(extractRoot, backupRoot);

            Directory.CreateDirectory(MetadataRoot);
            var local = new LocalVersionInfo
            {
                Version = remote.Version,
                ExePath = remote.ExePath,
                LaunchArgs = remote.LaunchArgs,
                InstalledAtUtc = DateTime.UtcNow
            };

            await File.WriteAllTextAsync(LocalVersionPath, JsonSerializer.Serialize(local, JsonOptions), cancellationToken);
            TryDeleteDirectory(backupRoot);
            status.Report("Installed.");
            progress.Report(100);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    public async Task PlayAsync(LauncherState state, CancellationToken cancellationToken)
    {
        LocalVersionInfo? local = state.Local;
        if (local is null && state.Remote is not null)
        {
            local = await ReadLocalVersionAsync(cancellationToken);
        }

        if (local is null)
        {
            throw new InvalidOperationException("No installed Cinder State build was found.");
        }

        string exePath = ResolveInstallPath(local.ExePath);
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("The installed Cinder State executable was not found.", exePath);
        }

        string launchArgs = !string.IsNullOrWhiteSpace(state.Remote?.LaunchArgs)
            ? state.Remote.LaunchArgs
            : local.LaunchArgs ?? "";

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = launchArgs,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? InstallRoot,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private async Task<RemoteVersionInfo> FetchRemoteVersionAsync(CancellationToken cancellationToken)
    {
        using Stream stream = await _httpClient.GetStreamAsync(VersionInfoUrl, cancellationToken);
        RemoteVersionInfo? remote = await JsonSerializer.DeserializeAsync<RemoteVersionInfo>(stream, cancellationToken: cancellationToken);
        if (remote is null)
        {
            throw new InvalidOperationException("Remote version file was empty or invalid.");
        }

        ValidateRemote(remote);
        return remote;
    }

    private async Task<LocalVersionInfo?> ReadLocalVersionAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(LocalVersionPath))
        {
            return null;
        }

        try
        {
            await using FileStream stream = File.OpenRead(LocalVersionPath);
            return await JsonSerializer.DeserializeAsync<LocalVersionInfo>(stream, cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task DownloadFileAsync(string url, string destinationPath, IProgress<int> progress, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        long? contentLength = response.Content.Headers.ContentLength;
        await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream output = File.Create(destinationPath);

        byte[] buffer = new byte[1024 * 128];
        long totalRead = 0;
        int read;

        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;

            if (contentLength is > 0)
            {
                progress.Report((int)Math.Clamp(totalRead * 100 / contentLength.Value, 0, 99));
            }
        }
    }

    private static async Task VerifySha256Async(string filePath, string expectedHash, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(filePath);
        byte[] hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        string actualHash = Convert.ToHexString(hashBytes);

        if (!string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Downloaded package failed SHA256 verification. Expected {expectedHash}, got {actualHash}.");
        }
    }

    private void ReplaceInstallFolder(string extractRoot, string backupRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(InstallRoot)!);

        if (Directory.Exists(InstallRoot))
        {
            if (Directory.Exists(backupRoot))
            {
                Directory.Delete(backupRoot, recursive: true);
            }

            Directory.Move(InstallRoot, backupRoot);
        }

        try
        {
            Directory.Move(extractRoot, InstallRoot);
        }
        catch
        {
            if (!Directory.Exists(InstallRoot) && Directory.Exists(backupRoot))
            {
                Directory.Move(backupRoot, InstallRoot);
            }

            throw;
        }
    }

    private string ResolveInstallPath(string relativePath)
    {
        return ResolveChildPath(InstallRoot, relativePath);
    }

    private static string ResolveStagedPath(string root, string relativePath)
    {
        return ResolveChildPath(root, relativePath);
    }

    private static string ResolveChildPath(string root, string relativePath)
    {
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException("Executable path must be relative to the install folder.");
        }

        string fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        string fullRoot = Path.GetFullPath(root);
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Executable path escapes the install folder.");
        }

        return fullPath;
    }

    private static bool IsRemoteNewer(string? localVersion, string remoteVersion)
    {
        if (string.IsNullOrWhiteSpace(localVersion))
        {
            return true;
        }

        if (Version.TryParse(localVersion, out Version? local) && Version.TryParse(remoteVersion, out Version? remote))
        {
            return remote > local;
        }

        return !string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateRemote(RemoteVersionInfo remote)
    {
        if (string.IsNullOrWhiteSpace(remote.Version))
        {
            throw new InvalidOperationException("Remote version is missing.");
        }

        if (string.IsNullOrWhiteSpace(remote.DownloadUrl))
        {
            throw new InvalidOperationException("Remote downloadUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(remote.ExePath))
        {
            throw new InvalidOperationException("Remote exePath is missing.");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup failures should not block launching or updating.
        }
    }
}
