param(
    [string]$RemoteName = "cinder-state-r2",
    [string]$BucketName = "cinder-state-launcher"
)

$ErrorActionPreference = "Stop"

function ConvertFrom-SecureStringToPlainText {
    param([System.Security.SecureString]$SecureValue)

    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

function Normalize-R2Endpoint {
    param([string]$Value)

    $normalized = $Value.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw "Account ID or S3 endpoint is required."
    }

    if ($normalized -match '^https?://') {
        $uri = [System.Uri]$normalized
        return "$($uri.Scheme)://$($uri.Host)".TrimEnd("/")
    }

    return "https://$normalized.r2.cloudflarestorage.com"
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

if (!(Get-Command "rclone" -ErrorAction SilentlyContinue)) {
    throw "rclone is not installed or is not available in PATH. Install it with: winget install Rclone.Rclone"
}

Write-Host ""
Write-Host "Cloudflare R2 rclone setup"
Write-Host "Remote name: $RemoteName"
Write-Host "Bucket:      $BucketName"
Write-Host ""
Write-Host "Use the S3 credentials from Cloudflare R2, not the public r2.dev URL."
Write-Host "Endpoint format: https://<ACCOUNT_ID>.r2.cloudflarestorage.com"
Write-Host ""

$endpointInput = Read-Host "Paste your Cloudflare Account ID or full S3 API endpoint"
$endpoint = Normalize-R2Endpoint $endpointInput
$accessKeyId = Read-Host "Paste your R2 Access Key ID"
$secretAccessKeySecure = Read-Host "Paste your R2 Secret Access Key" -AsSecureString
$secretAccessKey = ConvertFrom-SecureStringToPlainText $secretAccessKeySecure

if ([string]::IsNullOrWhiteSpace($accessKeyId)) {
    throw "Access Key ID is required."
}
if ([string]::IsNullOrWhiteSpace($secretAccessKey)) {
    throw "Secret Access Key is required."
}

Write-Host ""
Write-Host "Writing local rclone remote '$RemoteName'..."
Invoke-Checked "rclone" @(
    "config", "create", $RemoteName, "s3",
    "provider", "Cloudflare",
    "access_key_id", $accessKeyId,
    "secret_access_key", $secretAccessKey,
    "endpoint", $endpoint,
    "--non-interactive"
)

Write-Host ""
Write-Host "Testing bucket access..."
Invoke-Checked "rclone" @("lsf", "$RemoteName`:$BucketName", "--max-depth", "1")

$testFile = Join-Path $env:TEMP "cinder-state-r2-upload-test.txt"
"Cinder State R2 upload test $(Get-Date -Format o)" | Set-Content -Path $testFile -Encoding ascii

Write-Host ""
Write-Host "Testing upload and delete permissions..."
Invoke-Checked "rclone" @("copyto", $testFile, "$RemoteName`:$BucketName/.cinder-state-r2-upload-test.txt")
Invoke-Checked "rclone" @("deletefile", "$RemoteName`:$BucketName/.cinder-state-r2-upload-test.txt")
Remove-Item -LiteralPath $testFile -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "R2 upload credentials are configured."
Write-Host "The publish batch will upload to:"
Write-Host "  $RemoteName`:$BucketName/Cinder_State_Friend_Client_Win64.zip"
