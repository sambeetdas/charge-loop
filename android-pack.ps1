<#
.SYNOPSIS
    Builds an Android APK for ChargeLoop pointing to the specified backend server.
.EXAMPLE
    .\android-pack.ps1
    .\android-pack.ps1 -ServerUrl "http://13.233.63.106/api/v1" -BuildMode release
    .\android-pack.ps1 -ServerUrl "https://api.chargeloop.in/api/v1" -SplitPerAbi
#>
param(
    [string]$ServerUrl = "http://13.233.63.106/api/v1",
    [ValidateSet("debug", "release")]
    [string]$BuildMode = "release",
    [switch]$SplitPerAbi,
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$mobileDir = Join-Path $repoRoot "mobile"

if (-not (Test-Path $mobileDir)) {
    throw "Mobile project directory not found at: $mobileDir"
}

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    throw "Flutter CLI not found in PATH. Install Flutter and Android SDK first."
}

# Normalize server URL to end with /api/v1
$normalizedUrl = $ServerUrl.TrimEnd("/")
if (-not $normalizedUrl.EndsWith("/api/v1")) {
    $normalizedUrl = "$normalizedUrl/api/v1"
}

Write-Host "==> ChargeLoop Android APK Packager" -ForegroundColor Cyan
Write-Host "    Server API URL : $normalizedUrl"
Write-Host "    Build Mode     : $BuildMode"

Push-Location $mobileDir
try {
    $androidDir = Join-Path $mobileDir "android"
    if (-not (Test-Path $androidDir)) {
        Write-Host "==> mobile/android not found. Generating Android platform files..." -ForegroundColor Yellow
        flutter create --platforms=android .
        if ($LASTEXITCODE -ne 0) { throw "flutter create --platforms=android failed." }
    }

    # Ensure cleartext HTTP is allowed on Android 9+ when using an http:// server URL
    $manifestPath = Join-Path $androidDir "app\src\main\AndroidManifest.xml"
    if ((Test-Path $manifestPath) -and ($normalizedUrl -like "http://*")) {
        $manifest = Get-Content $manifestPath -Raw
        if ($manifest -notmatch 'android:usesCleartextTraffic') {
            $manifest = $manifest -replace '<application\b', '<application android:usesCleartextTraffic="true"'
            Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8
            Write-Host "==> Enabled android:usesCleartextTraffic=true in AndroidManifest.xml for HTTP server." -ForegroundColor Yellow
        }
    }

    # Disable Kotlin incremental cache on Windows to avoid file lock errors in plugins (e.g. mobile_scanner)
    $gradlePropsPath = Join-Path $androidDir "gradle.properties"
    if (Test-Path $gradlePropsPath) {
        $gradleProps = Get-Content $gradlePropsPath -Raw
        if ($gradleProps -notmatch 'kotlin\.incremental=') {
            Add-Content -Path $gradlePropsPath -Value "`nkotlin.incremental=false" -Encoding UTF8
            Write-Host "==> Added kotlin.incremental=false to android/gradle.properties." -ForegroundColor Yellow
        }
    }

    Write-Host "==> Fetching Flutter packages..." -ForegroundColor Cyan
    flutter pub get
    if ($LASTEXITCODE -ne 0) { throw "flutter pub get failed." }

    $buildArgs = @(
        "build", "apk",
        "--$BuildMode",
        "--dart-define=API_BASE_URL=$normalizedUrl"
    )
    if ($SplitPerAbi) {
        $buildArgs += "--split-per-abi"
    }

    Write-Host "==> Building Android APK (flutter $($buildArgs -join ' '))..." -ForegroundColor Cyan
    & flutter @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "flutter build apk failed." }

    $apkDir = Join-Path $mobileDir "build\app\outputs\flutter-apk"
    $destDir = Join-Path $repoRoot $OutputDir
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    $apks = Get-ChildItem -Path $apkDir -Filter "*.apk" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike "*unaligned*" }
    if (-not $apks) {
        throw "No APK files found in $apkDir"
    }

    foreach ($apk in $apks) {
        $destPath = Join-Path $destDir $apk.Name
        Copy-Item -Path $apk.FullName -Destination $destPath -Force
        Write-Host "==> APK ready: $destPath" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
