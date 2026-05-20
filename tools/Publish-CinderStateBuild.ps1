param(
    [string]$Version,

    [string]$ZipPath = "C:\Users\jinxu\Documents\Unreal Projects\Cinder_State\Saved\FriendClientPackage\Cinder_State_Friend_Client_Win64.zip",
    [string]$Repo = "cinderstategame/cinder-state-test-updates",
    [string]$ExePath = "Windows/Cinder_State.exe",
    [string]$LaunchArgs = "play.cinderstategame.com:7777 -log",
    [string]$Notes,
    [switch]$AutoIncrement,
    [switch]$ReplaceExistingReleaseAsset,
    [switch]$NoCommit,
    [switch]$UseR2,
    [string]$R2Remote = "cinder-state-r2",
    [string]$R2Bucket = "cinder-state-launcher",
    [string]$R2PublicUrl = "https://pub-f2c866d0b48d44e2a73269c91af359b0.r2.dev",
    [string]$R2ObjectName
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

    if (!(Get-Command "rclone" -ErrorAction SilentlyContinue)) {
        throw @"
Required command not found: rclone.

Install rclone once:
  winget install Rclone.Rclone

Then configure the Cloudflare R2 remote:
  rclone config

Recommended remote name:
  $RemoteName

Remote type:
  s3

Provider:
  Cloudflare

Endpoint format:
  https://<ACCOUNT_ID>.r2.cloudflarestorage.com
"@
    }

    $remotes = & rclone listremotes
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read rclone remotes. Run 'rclone config' and configure the Cloudflare R2 remote."
    }

    $expected = "$RemoteName`:"
    if (!($remotes | Where-Object { $_ -eq $expected })) {
        throw @"
Missing rclone remote '$RemoteName'.

Set it up once with your Cloudflare R2 S3 API credentials:
  rclone config

Recommended remote name:
  $RemoteName

Remote type:
  s3

Provider:
  Cloudflare

Endpoint format:
  https://<ACCOUNT_ID>.r2.cloudflarestorage.com

The public bucket URL is already configured in this publish script:
  $R2PublicUrl
"@
    }
}

function Test-GitHubReleaseExists {
    param(
        [string]$Tag,
        [string]$Repository
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "SilentlyContinue"
        & gh release view $Tag --repo $Repository *> $null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Normalize-R2ObjectName {
    param(
        [string]$Value,
        [string]$Fallback
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $Value = $Fallback
    }

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

function Normalize-Version {
    param([string]$Value)

    $normalized = $Value.Trim()
    if ($normalized.StartsWith("v", [StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(1)
    }

    if ($normalized -notmatch '^\d+(\.\d+){1,3}$') {
        throw "Version must look like 0.0.0.1 or v0.0.0.1."
    }

    $parts = @($normalized.Split('.') | ForEach-Object { [int]$_ })
    while ($parts.Count -lt 4) {
        $parts += 0
    }

    return ($parts[0..3] -join ".")
}

function Get-Next-Version {
    param([string]$CurrentVersion)

    $normalized = Normalize-Version $CurrentVersion
    $parts = @($normalized.Split('.') | ForEach-Object { [int]$_ })
    $parts[3]++
    return ($parts -join ".")
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$publisherStatePath = Join-Path $repoRoot "publisher-version.txt"

if ($AutoIncrement) {
    if (!(Test-Path $publisherStatePath)) {
        "0.0.0.0" | Set-Content -Path $publisherStatePath -Encoding ascii
    }

    $currentPublisherVersion = Get-Content -Raw $publisherStatePath
    $versionNumber = Get-Next-Version $currentPublisherVersion
}
elseif (![string]::IsNullOrWhiteSpace($Version)) {
    $versionNumber = Normalize-Version $Version
}
else {
    throw "Provide -Version 0.0.0.1 or use -AutoIncrement."
}

$tag = "v$versionNumber"
$resolvedZip = Resolve-Path $ZipPath
$zipName = Split-Path $resolvedZip -Leaf
$r2ObjectName = Normalize-R2ObjectName -Value $R2ObjectName -Fallback $zipName
$downloadUrl = if ($UseR2) {
    Join-PublicR2Url -BaseUrl $R2PublicUrl -ObjectName $r2ObjectName
}
else {
    "https://github.com/$Repo/releases/download/$tag/$zipName"
}

if ([string]::IsNullOrWhiteSpace($Notes)) {
    $Notes = "Private tester build $versionNumber."
}

Push-Location $repoRoot
try {
    Assert-Command "git"

    if ($UseR2) {
        Write-Host "Checking rclone R2 remote '$R2Remote'..."
        Assert-RcloneRemote $R2Remote
    }
    else {
        Assert-Command "gh"

        Write-Host "Checking GitHub authentication..."
        Invoke-Checked "gh" @("auth", "status")
    }

    Write-Host "Checking git working tree..."
    $status = git status --short
    if ($status) {
        $allowedDirty = $status | Where-Object { $_ -match '^\s*M\s+\.gitignore$' -or $_ -match '^\?\?\s+tools/' }
        $unexpectedDirty = $status | Where-Object { $_ -notin $allowedDirty }
        if ($unexpectedDirty -and !$NoCommit) {
            throw "Working tree has unrelated pending changes. Commit/stash them first, or rerun with -NoCommit.`n$($unexpectedDirty -join [Environment]::NewLine)"
        }
    }

    if ($UseR2) {
        Write-Host "Uploading/replacing R2 object..."
        Write-Host "Bucket: $R2Bucket"
        Write-Host "Object: $r2ObjectName"
        Invoke-Checked "rclone" @(
            "copyto",
            $resolvedZip.Path,
            "$R2Remote`:$R2Bucket/$r2ObjectName",
            "--progress"
        )
    }
    else {
        Write-Host "Preparing release $tag..."
        $releaseExists = Test-GitHubReleaseExists -Tag $tag -Repository $Repo

        if ($releaseExists) {
            if (!$ReplaceExistingReleaseAsset) {
                throw "Release $tag already exists. Use -ReplaceExistingReleaseAsset to replace the ZIP asset."
            }

            Write-Host "Release exists. Uploading/replacing asset..."
            Invoke-Checked "gh" @("release", "upload", $tag, $resolvedZip.Path, "--repo", $Repo, "--clobber")
        }
        else {
            Write-Host "Creating release and uploading asset..."
            Invoke-Checked "gh" @(
                "release", "create", $tag, $resolvedZip.Path,
                "--repo", $Repo,
                "--title", "Cinder State Test $tag",
                "--notes", $Notes
            )
        }
    }

    Write-Host "Calculating SHA256..."
    $hash = (Get-FileHash $resolvedZip.Path -Algorithm SHA256).Hash

    Write-Host "Updating version.json..."
    $versionJsonPath = Join-Path $repoRoot "version.json"
    $versionJson = [ordered]@{
        version = $versionNumber
        downloadUrl = $downloadUrl
        exePath = $ExePath
        sha256 = $hash
        launchArgs = $LaunchArgs
        notes = $Notes
    }

    $versionJson |
        ConvertTo-Json -Depth 4 |
        Set-Content -Path $versionJsonPath -Encoding utf8

    if ($AutoIncrement) {
        $versionNumber | Set-Content -Path $publisherStatePath -Encoding ascii
    }

    if (!$NoCommit) {
        Write-Host "Committing and pushing version.json..."
        if ($AutoIncrement) {
            Invoke-Checked "git" @("add", "version.json", "publisher-version.txt")
        }
        else {
            Invoke-Checked "git" @("add", "version.json")
        }
        Invoke-Checked "git" @("commit", "-m", "Publish Cinder State test build $tag")
        Invoke-Checked "git" @("push")
    }

    Write-Host ""
    Write-Host "Published Cinder State test build $versionNumber"
    if ($UseR2) {
        Write-Host "R2 object: $R2Remote`:$R2Bucket/$r2ObjectName"
    }
    Write-Host "Download URL: $downloadUrl"
    Write-Host "SHA256: $hash"
    Write-Host "version.json: https://raw.githubusercontent.com/$Repo/main/version.json"
}
finally {
    Pop-Location
}
