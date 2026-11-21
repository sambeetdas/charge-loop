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

# Normalize server URL and derive frontend WebView URL
$baseHostUrl = $ServerUrl.TrimEnd("/") -replace '/api/v1$', ''
$normalizedUrl = "$baseHostUrl/api/v1"
$appUrl = "$baseHostUrl/?app=1"

Write-Host "==> ChargeLoop Android APK Packager" -ForegroundColor Cyan
Write-Host "    Frontend App URL : $appUrl"
Write-Host "    Server API URL   : $normalizedUrl"
Write-Host "    Build Mode       : $BuildMode"

Push-Location $mobileDir
try {
    $androidDir = Join-Path $mobileDir "android"
    if (-not (Test-Path $androidDir)) {
        Write-Host "==> mobile/android not found. Generating Android platform files..." -ForegroundColor Yellow
        flutter create --platforms=android .
        if ($LASTEXITCODE -ne 0) { throw "flutter create --platforms=android failed." }
    }

    # Ensure INTERNET permission and cleartext HTTP are allowed on Android 9+ in release builds
    $manifestPath = Join-Path $androidDir "app\src\main\AndroidManifest.xml"
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw
        $manifestChanged = $false
        if ($manifest -notmatch 'android\.permission\.INTERNET') {
            $manifest = $manifest -replace '(<manifest[^>]*>)', "`$1`n    <uses-permission android:name=`"android.permission.INTERNET`" />"
            $manifestChanged = $true
            Write-Host "==> Added android.permission.INTERNET to AndroidManifest.xml." -ForegroundColor Yellow
        }
        if (($normalizedUrl -like "http://*") -and ($manifest -notmatch 'android:usesCleartextTraffic')) {
            $manifest = $manifest -replace '<application\b', '<application android:usesCleartextTraffic="true"'
            $manifestChanged = $true
            Write-Host "==> Enabled android:usesCleartextTraffic=true in AndroidManifest.xml for HTTP server." -ForegroundColor Yellow
        }
        if ($manifestChanged) {
            Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8
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
        "--dart-define=APP_URL=$appUrl",
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
