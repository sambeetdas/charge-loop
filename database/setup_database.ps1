<#
.SYNOPSIS
    Automated PostgreSQL Setup Script for ChargeLoop EV Battery Swapping Network.

.DESCRIPTION
    This script locates PostgreSQL (psql.exe), verifies connection, creates the
    'chargeloop' database if missing, and executes the schema definition and seed data scripts.

.PARAMETER PgHost
    PostgreSQL server hostname or IP (default: localhost).

.PARAMETER PgPort
    PostgreSQL server port (default: 5432).

.PARAMETER PgUser
    PostgreSQL administrative username (default: postgres).

.PARAMETER PgPassword
    PostgreSQL administrative password (default: Sambeet@123).

.PARAMETER DbName
    Target database name (default: chargeloop).

.PARAMETER Reset
    Switch to drop and recreate all tables from scratch.

.EXAMPLE
    .\setup_database.ps1
    .\setup_database.ps1 -PgUser postgres -PgPassword "YourPassword" -Reset
#>

param(
    [string]$PgHost = "localhost",
    [int]$PgPort = 5432,
    [string]$PgUser = "postgres",
    [string]$PgPassword = "Sambeet@123",
    [string]$DbName = "chargeloop",
    [switch]$Reset
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "⚡ ChargeLoop PostgreSQL Automated Setup & Migration" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Locate psql.exe
$PsqlCmd = $null
$PsqlCmdObj = Get-Command psql.exe -ErrorAction SilentlyContinue
if ($PsqlCmdObj) {
    $PsqlCmd = $PsqlCmdObj.Source
} else {
    $SearchPaths = @(
        "${env:ProgramFiles}\PostgreSQL\*\bin\psql.exe",
        "${env:ProgramFiles(x86)}\PostgreSQL\*\bin\psql.exe",
        "C:\PostgreSQL\*\bin\psql.exe"
    )
    foreach ($pathPattern in $SearchPaths) {
        $found = Resolve-Path $pathPattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $PsqlCmd = $found.Path
            break
        }
    }
}

if (-not $PsqlCmd) {
    Write-Host "❌ psql.exe not found on system." -ForegroundColor Red
    Write-Host "Please ensure PostgreSQL is installed or specify the path to psql.exe in your PATH." -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Found psql binary: $PsqlCmd" -ForegroundColor DarkGray
Write-Host "Connecting to $PgHost`:$PgPort as user '$PgUser'..." -ForegroundColor DarkGray

$env:PGPASSWORD = $PgPassword

# 2. Check if target database exists; if not, create it
try {
    $dbCheck = & $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d postgres -t -A -c "SELECT 1 FROM pg_database WHERE datname='$DbName';" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Could not connect to PostgreSQL server: $dbCheck" -ForegroundColor Red
        exit 1
    }

    if ($dbCheck.Trim() -ne "1") {
        Write-Host "Database '$DbName' does not exist. Creating..." -ForegroundColor Yellow
        & $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d postgres -c "CREATE DATABASE $DbName;" | Out-Null
        Write-Host "✓ Database '$DbName' created successfully." -ForegroundColor Green
    } else {
        Write-Host "✓ Database '$DbName' already exists." -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Error checking/creating database: $_" -ForegroundColor Red
    exit 1
}

# 3. Optional Reset
if ($Reset) {
    Write-Host "⚠️  Reset requested. Dropping existing tables..." -ForegroundColor Yellow
    $ResetSql = Join-Path $ScriptDir "03_reset_database.sql"
    if (Test-Path $ResetSql) {
        & $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d $DbName -f $ResetSql | Out-Null
        Write-Host "✓ Existing tables dropped." -ForegroundColor Green
    }
}

# 4. Apply Schema
$SchemaSql = Join-Path $ScriptDir "01_init_schema.sql"
Write-Host "Applying schema from 01_init_schema.sql..." -ForegroundColor Cyan
try {
    & $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d $DbName -f $SchemaSql | Out-Null
    Write-Host "✓ Core schema and performance indexes applied successfully." -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to apply schema: $_" -ForegroundColor Red
    exit 1
}

# 5. Apply Seed Data
$SeedSql = Join-Path $ScriptDir "02_seed_data.sql"
Write-Host "Seeding initial partners, stations, drivers, and batteries from 02_seed_data.sql..." -ForegroundColor Cyan
try {
    & $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d $DbName -f $SeedSql | Out-Null
    Write-Host "✓ Initial seed data populated successfully." -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to apply seed data: $_" -ForegroundColor Red
    exit 1
}

# 6. Verify Tables & Counts
Write-Host "`n📊 Database Summary:" -ForegroundColor Magenta
$SummaryQuery = @"
SELECT 
    schemaname || '.' || relname AS table_name,
    n_live_tup AS estimated_rows
FROM pg_stat_user_tables
ORDER BY relname;
"@

& $PsqlCmd -h $PgHost -p $PgPort -U $PgUser -d $DbName -c $SummaryQuery

Write-Host "`n🎉 PostgreSQL setup completed successfully for ChargeLoop!" -ForegroundColor Green
Write-Host "Connection string: Host=$PgHost;Port=$PgPort;Database=$DbName;Username=$PgUser;Password=***" -ForegroundColor DarkGray
