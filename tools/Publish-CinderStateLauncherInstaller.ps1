param(
    [switch]$UploadToR2,
    [string]$R2Remote = "cinder-state-r2",
    [string]$R2Bucket = "cinder-state-launcher",
    [string]$R2PublicUrl = "https://pub-f2c866d0b48d44e2a73269c91af359b0.r2.dev",
    [string]$R2ObjectName = "CinderStateLauncherInstaller.exe"
)

$ErrorActionPreference = "Stop"

function Assert-Command {
    param([string]$Name)

    if (!(Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE"
    }
}

function Assert-RcloneRemote {
    param([string]$RemoteName)

    Assert-Command "rclone"

    $remotes = & rclone listremotes
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read rclone remotes. Run SetupR2UploadCredentials.bat first."
    }

    $expected = "$RemoteName`:"
    if (!($remotes | Where-Object { $_ -eq $expected })) {
        throw "Missing rclone remote '$RemoteName'. Run SetupR2UploadCredentials.bat first."
    }
}

function Normalize-R2ObjectName {
    param([string]$Value)

    $normalized = $Value.Trim().Replace("\", "/").TrimStart("/")
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw "R2 object name cannot be empty."
    }

    return $normalized
}

function ConvertTo-UrlPath {
    param([string]$ObjectName)

    $segments = $ObjectName.Split("/") | ForEach-Object {
        [System.Uri]::EscapeDataString($_)
    }

    return ($segments -join "/")
}

function Join-PublicR2Url {
    param(
        [string]$BaseUrl,
        [string]$ObjectName
    )

    return "$($BaseUrl.TrimEnd('/'))/$(ConvertTo-UrlPath $ObjectName)"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$launcherProject = Join-Path $repoRoot "src\CinderStateLauncher\CinderStateLauncher.csproj"
$installerProject = Join-Path $repoRoot "src\CinderStateLauncherInstaller\CinderStateLauncherInstaller.csproj"
$launcherPublishDir = Join-Path $repoRoot "publish\win-x64"
$installerPublishDir = Join-Path $repoRoot "dist\installer"
$launcherExePath = Join-Path $launcherPublishDir "CinderStateLauncher.exe"
$installerExePath = Join-Path $installerPublishDir "CinderStateLauncherInstaller.exe"
$hashPath = Join-Path $installerPublishDir "CinderStateLauncherInstaller.sha256.txt"
$r2ObjectName = Normalize-R2ObjectName $R2ObjectName
$installerUrl = Join-PublicR2Url -BaseUrl $R2PublicUrl -ObjectName $r2ObjectName

Push-Location $repoRoot
try {
    Assert-Command "dotnet"

    if (Get-Command "git" -ErrorAction SilentlyContinue) {
        $status = git status --short
        if ($status) {
            Write-Host "Note: working tree has pending changes. The installer will be built from the current files."
        }
    }

    if ($UploadToR2) {
        Write-Host "Checking rclone R2 remote '$R2Remote'..."
        Assert-RcloneRemote $R2Remote
    }

    Write-Host "Publishing launcher payload..."
    Invoke-Checked "dotnet" @(
        "publish", $launcherProject,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-o", $launcherPublishDir
    )

    if (!(Test-Path $launcherExePath)) {
        throw "Launcher publish did not create $launcherExePath"
    }

    Write-Host "Publishing installer..."
    Invoke-Checked "dotnet" @(
        "publish", $installerProject,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-o", $installerPublishDir
    )

    if (!(Test-Path $installerExePath)) {
        throw "Installer publish did not create $installerExePath"
    }

    $hash = (Get-FileHash $installerExePath -Algorithm SHA256).Hash
    "$hash  CinderStateLauncherInstaller.exe" | Set-Content -Path $hashPath -Encoding ascii

    if ($UploadToR2) {
        Write-Host "Uploading/replacing R2 launcher installer..."
        Write-Host "Bucket: $R2Bucket"
        Write-Host "Object: $r2ObjectName"
        Invoke-Checked "rclone" @(
            "copyto",
            $installerExePath,
            "$R2Remote`:$R2Bucket/$r2ObjectName",
            "--progress"
        )

        Write-Host "Checking public installer URL..."
        $response = Invoke-WebRequest -Uri $installerUrl -Method Head -UseBasicParsing
        if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
            throw "Public installer URL returned HTTP $($response.StatusCode): $installerUrl"
        }
    }

    Write-Host ""
    Write-Host "Launcher installer ready."
    Write-Host "Local installer: $installerExePath"
    Write-Host "SHA256: $hash"
    if ($UploadToR2) {
        Write-Host "Download URL: $installerUrl"
    }
}
finally {
    Pop-Location
}
