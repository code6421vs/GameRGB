[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$openRgbVersion = "1.1"
$openRgbUrl = "https://codeberg.org/OpenRGB/OpenRGB/releases/download/release_1.1/OpenRGB_1.1_Windows_64_8e16de2.zip"
$openRgbSha256 = "63291726C6FC84EB479EF2EE752E97E1187F6CE9C49FD27EEE40DED588A8172A"
$pawnIoVersion = "2.2.0"
$pawnIoUrl = "https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe"
$pawnIoSha256 = "1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032"

$repoRoot = $PSScriptRoot
$cacheRoot = Join-Path $repoRoot ".deps-cache"
$openRgbArchive = Join-Path $cacheRoot "OpenRGB-$openRgbVersion-win-x64.zip"
$pawnIoArchive = Join-Path $cacheRoot "PawnIO-$pawnIoVersion-setup.exe"
$openRgbTarget = Join-Path $repoRoot "GameRgb\OpenRGB"
$pawnIoTarget = Join-Path $repoRoot "GameRgb\DriverInstallers\PawnIO_setup.exe"
$publishTarget = Join-Path $repoRoot "artifacts\GameRGB"

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Get-VerifiedFile {
    param(
        [Parameter(Mandatory)] [string]$Uri,
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Sha256
    )

    if (Test-Path -LiteralPath $Path) {
        $existingHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        if ($existingHash -eq $Sha256) {
            Write-Host "Using verified cache: $Path"
            return
        }
        Remove-Item -LiteralPath $Path -Force
    }

    Write-Host "Downloading $Uri"
    Invoke-WebRequest -Uri $Uri -OutFile $Path -UseBasicParsing
    $actualHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($actualHash -ne $Sha256) {
        Remove-Item -LiteralPath $Path -Force
        throw "SHA-256 mismatch for $Uri. Expected $Sha256, got $actualHash."
    }
}

if ($env:OS -ne "Windows_NT") {
    throw "GameRGB can only be built and run on Windows."
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

Write-Step "Preparing OpenRGB $openRgbVersion"
Get-VerifiedFile -Uri $openRgbUrl -Path $openRgbArchive -Sha256 $openRgbSha256
$openRgbMarker = Join-Path $openRgbTarget ".gamergb-version"
$expectedMarker = "OpenRGB=$openRgbVersion`nSHA256=$openRgbSha256"
$currentMarker = if (Test-Path -LiteralPath $openRgbMarker) {
    (Get-Content -LiteralPath $openRgbMarker -Raw).Trim()
} else { "" }

if ($Force -or $currentMarker -ne $expectedMarker.Trim() -or -not (Test-Path (Join-Path $openRgbTarget "OpenRGB.exe"))) {
    $extractRoot = Join-Path $cacheRoot "openrgb-extract"
    if (Test-Path -LiteralPath $extractRoot) { Remove-Item -LiteralPath $extractRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $extractRoot | Out-Null
    Expand-Archive -LiteralPath $openRgbArchive -DestinationPath $extractRoot -Force
    $openRgbExe = Get-ChildItem -LiteralPath $extractRoot -Filter "OpenRGB.exe" -File -Recurse | Select-Object -First 1
    if (-not $openRgbExe) { throw "The OpenRGB archive did not contain OpenRGB.exe." }
    if (Test-Path -LiteralPath $openRgbTarget) { Remove-Item -LiteralPath $openRgbTarget -Recurse -Force }
    New-Item -ItemType Directory -Path $openRgbTarget | Out-Null
    Copy-Item -Path (Join-Path $openRgbExe.DirectoryName "*") -Destination $openRgbTarget -Recurse -Force
    Set-Content -LiteralPath $openRgbMarker -Value $expectedMarker -Encoding ASCII
    Remove-Item -LiteralPath $extractRoot -Recurse -Force
}

Write-Step "Preparing signed PawnIO $pawnIoVersion installer"
Get-VerifiedFile -Uri $pawnIoUrl -Path $pawnIoArchive -Sha256 $pawnIoSha256
$signature = Get-AuthenticodeSignature -LiteralPath $pawnIoArchive
if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
    $signature.SignerCertificate.Subject -notmatch "CN=namazso\.eu") {
    throw "PawnIO Authenticode signature is not valid or has an unexpected signer."
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $pawnIoTarget) | Out-Null
if ($Force -or -not (Test-Path -LiteralPath $pawnIoTarget) -or
    (Get-FileHash -LiteralPath $pawnIoTarget -Algorithm SHA256).Hash -ne $pawnIoSha256) {
    Copy-Item -LiteralPath $pawnIoArchive -Destination $pawnIoTarget -Force
}

if ($SkipBuild) {
    Write-Host "`nDependencies are ready. Build skipped." -ForegroundColor Green
    exit 0
}

Write-Step "Checking .NET SDK"
$dotnetVersionText = (& dotnet --version 2>$null)
if ($LASTEXITCODE -ne 0 -or -not $dotnetVersionText) {
    throw ".NET 10 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}
$dotnetMajor = [int](($dotnetVersionText -split '\.')[0])
if ($dotnetMajor -lt 10) { throw ".NET 10 SDK or newer is required; found $dotnetVersionText." }
Write-Host "Using .NET SDK $dotnetVersionText"

Write-Step "Restoring NuGet packages"
& dotnet restore (Join-Path $repoRoot "GameRgb.slnx")
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Step "Publishing GameRGB ($Configuration, win-x64)"
& dotnet publish (Join-Path $repoRoot "GameRgb\GameRgb.csproj") `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained false `
    --output $publishTarget
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "`nGameRGB is ready:" -ForegroundColor Green
Write-Host (Join-Path $publishTarget "GameRgb.exe")
Write-Host "Run it as administrator. PawnIO is installed only if you approve the first-run prompt."
