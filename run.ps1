# ==============================================================================
#  ChargeLoop EV Battery Swapping Network - Start Script
#  Launches the .NET 11 Backend API and opens the Mobile App Client
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       ⚡ ChargeLoop EV Battery Swapping Network ⚡       " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$workspaceRoot = $PSScriptRoot
if (-not $workspaceRoot) { $workspaceRoot = Get-Location }

# 1. Check .NET SDK
Write-Host "[1/4] Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ✓ .NET SDK $dotnetVersion detected" -ForegroundColor Green
} catch {
    Write-Host "  ✗ .NET SDK not found. Please install .NET 11 SDK." -ForegroundColor Red
    exit 1
}

# 2. Check PostgreSQL Database Connection
Write-Host "[2/4] Verifying PostgreSQL connection on localhost:5432..." -ForegroundColor Yellow
$pgTest = Test-NetConnection -ComputerName "localhost" -Port 5432 -WarningAction SilentlyContinue
if ($pgTest.TcpTestSucceeded) {
    Write-Host "  ✓ PostgreSQL is online at localhost:5432 (chargeloop)" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Warning: PostgreSQL port 5432 is not reachable." -ForegroundColor DarkYellow
    Write-Host "    Make sure PostgreSQL service is running." -ForegroundColor DarkGray
}

# 3. Start Backend API Process
Write-Host "[3/4] Starting ChargeLoop .NET 11 Backend Server..." -ForegroundColor Yellow

$backendDir = Join-Path $workspaceRoot "backend\ChargeLoop.WebApi"
$serverProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project `"$backendDir`" --urls http://localhost:5000" `
    -PassThru `
    -NoNewWindow

Write-Host "  ✓ Server process started (PID: $($serverProcess.Id))" -ForegroundColor Green
Write-Host "  Waiting for backend to initialize on http://localhost:5000..." -ForegroundColor DarkGray

# Wait up to 15 seconds for server to be responsive
$isReady = $false
$maxRetries = 15
for ($i = 1; $i -le $maxRetries; $i++) {
    Start-Sleep -Seconds 1
    try {
        $res = Invoke-WebRequest -Uri "http://localhost:5000/" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($res.StatusCode -eq 200) {
            $isReady = $true
            break
        }
    } catch {
        # Keep waiting
    }
}

if ($isReady) {
    Write-Host "  ✓ Backend API and Mobile Web Client are ready!" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Server is still starting or initializing." -ForegroundColor DarkYellow
}

# 4. Launch Mobile App in Browser
Write-Host "[4/4] Opening ChargeLoop Mobile App..." -ForegroundColor Yellow
Start-Process "http://localhost:5000"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "             🚀 SERVICES ARE RUNNING LIVE               " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  📱 Mobile App Preview   : " -NoNewline; Write-Host "http://localhost:5000" -ForegroundColor Cyan
Write-Host "  📜 Swagger OpenAPI Docs : " -NoNewline; Write-Host "http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "  ⚡ SignalR Live Hub     : " -NoNewline; Write-Host "http://localhost:5000/hubs/stations" -ForegroundColor Cyan
Write-Host "  🗄️ Database             : " -NoNewline; Write-Host "localhost:5432/chargeloop" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Tip: Press Ctrl+C in this terminal to stop the backend." -ForegroundColor DarkGray
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""

# Monitor process until user exits
try {
    while (-not $serverProcess.HasExited) {
        Start-Sleep -Seconds 1
    }
} finally {
    Write-Host "`nStopping ChargeLoop server..." -ForegroundColor Yellow
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Server stopped cleanly." -ForegroundColor Green
    }
}
