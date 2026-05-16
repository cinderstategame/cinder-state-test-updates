using System.Text.Json.Serialization;

namespace CinderStateLauncher;

internal sealed class RemoteVersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("exePath")]
    public string ExePath { get; set; } = "";

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("launchArgs")]
    public string? LaunchArgs { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

internal sealed class LocalVersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("exePath")]
    public string ExePath { get; set; } = "";

    [JsonPropertyName("launchArgs")]
    public string? LaunchArgs { get; set; }

    [JsonPropertyName("installedAtUtc")]
    public DateTime InstalledAtUtc { get; set; }
}

internal sealed class LauncherState
{
    public LocalVersionInfo? Local { get; init; }
    public RemoteVersionInfo? Remote { get; init; }
    public bool HasInstalledBuild { get; init; }
    public bool NeedsUpdate { get; init; }
}
