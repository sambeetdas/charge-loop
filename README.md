# ChargeLoop EV Battery Swapping Application Suite

ChargeLoop operates an enterprise Battery-as-a-Service (BaaS) swapping network for electric 2-wheelers and 3-wheelers. Drivers exchange a depleted battery for a fully charged one in under **2 minutes** at partner-run stations.

---

## 🏗️ System Architecture

* **Mobile App Client**: **Flutter (Dart)** — Multiplatform codebase designed for **Android first** (budget device friendly, high-contrast sunlight readability) and seamlessly deployable to **iOS & macOS**.
* **Backend Core API**: **ASP.NET Core (.NET 11, C#)** — High-throughput Clean Architecture API with:
  * **Real-time SignalR Hub** (`/hubs/stations`) for instant battery count and swap state broadcasting.
  * **Swap Engine** for sub-1.5s battery validation and 2-minute physical swap verification.
  * **Double-entry Wallet & Commission Ledger** for drivers and station partners.
  * **Geospatial Station Discovery** (Haversine & PostGIS compatible).
  * **IoT Telemetry Ingestion** for battery state of charge (SOC), cell temperature, and anti-theft monitoring.
* **Database & Caching**: **PostgreSQL 16 with PostGIS** + **Redis 7** (via `docker-compose.yml`).

---

## 📁 Repository Structure

```
ChargeLoop/
├── database/                             # Database migrations, schema DDL, seed data & setup scripts
│   ├── 01_init_schema.sql                # Core schema DDL & performance indexes
│   ├── 02_seed_data.sql                  # Seed partners, stations, drivers, vehicles & batteries
│   ├── 03_reset_database.sql             # Teardown & reset script
│   ├── setup_database.ps1                # Automated PowerShell setup script
│   ├── setup_database.sh                 # Automated Bash setup script
│   └── README.md                         # Comprehensive PostgreSQL setup documentation
├── requirement/
│   └── 2026-11-13/
│       └── ChargeLoop_BRD.docx           # Business Requirements Document
├── doc/                                  # Project documentation (PRD, Requirements, Architecture, Tasks)
├── backend/                              # .NET 11 Clean Architecture Solution
│   ├── ChargeLoop.Domain/                # Entities (Driver, Vehicle, Battery, Station, Swap, Partner)
│   ├── ChargeLoop.Application/           # DTOs, Service Interfaces
│   ├── ChargeLoop.Infrastructure/        # PostgreSQL EF Core DbContext, Services, SignalR event bridge
│   └── ChargeLoop.WebApi/                # Controllers (Auth, Stations, Swaps, Wallet, Support), SignalR Hubs
├── mobile/                               # Flutter Multiplatform Driver & Vendor App
│   ├── pubspec.yaml
│   └── lib/
│       ├── core/theme/                   # High-contrast electric UI theme
│       ├── core/network/                 # REST & SignalR API client
│       ├── models/                       # Data models (Driver, Vendor, Station, Battery)
│       ├── widgets/                      # Custom Battery Gauge (SOC %), status badges
│       └── screens/                      # Auth, Stations Map, 2-Min Swap, Wallet, Vendor Operations & Profiles
└── docker-compose.yml                    # PostgreSQL + PostGIS & Redis services
```

---

## 🚀 Getting Started

### ⚡ 1-Click Launch (Backend + Mobile App)

Simply run the PowerShell script (or double-click `run.bat`):

```powershell
.\run.ps1
```

This script:
1. Verifies the .NET 11 SDK and PostgreSQL connection (`localhost:5432`).
2. Starts the backend server on `http://localhost:5000`.
3. Opens the Mobile App in your default web browser automatically.

---

### Manual Launch (Step-by-Step)

#### 1. Setup PostgreSQL Database

Initialize the `chargeloop` database and seed data with one command:

```powershell
# Using the automated setup script
.\database\setup_database.ps1
```

Or execute directly with `psql`:
```bash
psql -h localhost -p 5432 -U postgres -d postgres -c "CREATE DATABASE chargeloop;"
psql -h localhost -p 5432 -U postgres -d chargeloop -f database/01_init_schema.sql
psql -h localhost -p 5432 -U postgres -d chargeloop -f database/02_seed_data.sql
```
*(See [`database/README.md`](database/README.md) for full guide, table dictionary, and reset instructions).*

---

#### 2. Run the .NET 11 Backend API

Ensure .NET 11 SDK is installed:

```powershell
cd backend
dotnet run --project ChargeLoop.WebApi
```

* **Interactive Swagger UI**: Open [http://localhost:5000](http://localhost:5000) or [http://localhost:5000/swagger](http://localhost:5000/swagger) in your browser.
* **Real-time SignalR Hub**: Available at `http://localhost:5000/hubs/stations`.

---

#### 3. Run with Docker Compose (Alternative)

```powershell
docker compose up -d
```

---

### 3. Run the Flutter Mobile App

Navigate to the `mobile` folder:

```powershell
cd mobile

# Get dependencies
flutter pub get

# Run on connected Android device or emulator
flutter run -d android

# Or run on macOS Desktop / Chrome for instant preview
flutter run -d macos
# or
flutter run -d chrome
```

---

## ⚡ Core 2-Minute Battery Swap Flow

1. **Station Arrival**: Driver arrives at station (e.g. *Janakpuri Swap Hub*).
2. **Scan Handshake**: Driver scans station QR code (`POST /api/v1/swaps/initiate`).
3. **Automated Validation (< 1.5s)**: Backend verifies battery serials, cell temperature, and minimum 90% SOC.
4. **Physical Exchange**: Partner places new 100% charged battery into driver's EV (~60–90s).
5. **Settlement**: Partner confirms swap (`POST /api/v1/swaps/confirm`). Driver wallet deducted (₹118 with GST) and partner instantly credited commission (₹25).

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for full details.

Copyright (c) 2026 Sambeet Das & ChargeLoop Energy Solutions.
