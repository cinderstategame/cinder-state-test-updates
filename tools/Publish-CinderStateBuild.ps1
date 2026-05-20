param(
    [string]$Version,

    [string]$ZipPath = "C:\Users\jinxu\Documents\Unreal Projects\Cinder_State\Saved\FriendClientPackage\Cinder_State_Friend_Client_Win64.zip",
    [string]$Repo = "cinderstategame/cinder-state-test-updates",
    [string]$ExePath = "Windows/Cinder_State.exe",
    [string]$LaunchArgs = "play.cinderstategame.com:7777 -log",
    [string]$Notes,
    [switch]$AutoIncrement,
    [switch]$ReplaceExistingReleaseAsset,
    [switch]$NoCommit
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
$downloadUrl = "https://github.com/$Repo/releases/download/$tag/$zipName"

if ([string]::IsNullOrWhiteSpace($Notes)) {
    $Notes = "Private tester build $versionNumber."
}

Push-Location $repoRoot
try {
    Assert-Command "git"
    Assert-Command "gh"

    Write-Host "Checking GitHub authentication..."
    Invoke-Checked "gh" @("auth", "status")

    Write-Host "Checking git working tree..."
    $status = git status --short
    if ($status) {
        $allowedDirty = $status | Where-Object { $_ -match '^\s*M\s+\.gitignore$' -or $_ -match '^\?\?\s+tools/' }
        $unexpectedDirty = $status | Where-Object { $_ -notin $allowedDirty }
        if ($unexpectedDirty -and !$NoCommit) {
            throw "Working tree has unrelated pending changes. Commit/stash them first, or rerun with -NoCommit.`n$($unexpectedDirty -join [Environment]::NewLine)"
        }
    }

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
    Write-Host "ZIP: $downloadUrl"
    Write-Host "SHA256: $hash"
    Write-Host "version.json: https://raw.githubusercontent.com/$Repo/main/version.json"
}
finally {
    Pop-Location
}
