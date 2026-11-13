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
├── requirement/
│   └── 2026-11-13/
│       └── ChargeLoop_BRD.docx           # Business Requirements Document
├── backend/                              # .NET 8 Clean Architecture Solution
│   ├── ChargeLoop.slnx
│   ├── ChargeLoop.Domain/                # Entities (Driver, Vehicle, Battery, Station, Swap)
│   ├── ChargeLoop.Application/           # DTOs, Service Interfaces
│   ├── ChargeLoop.Infrastructure/        # Services, In-memory seed store, SignalR event bridge
│   └── ChargeLoop.WebApi/                # Controllers (Auth, Stations, Swaps, Wallet, Telemetry), SignalR Hubs
├── mobile/                               # Flutter Multiplatform Driver App
│   ├── pubspec.yaml
│   └── lib/
│       ├── core/theme/                   # High-contrast electric UI theme
│       ├── core/network/                 # REST & SignalR API client with offline fallback
│       ├── models/                       # Data models
│       ├── widgets/                      # Custom Battery Gauge (SOC %), status badges
│       └── screens/                      # Auth, Stations Map, 2-Min Swap, Wallet, Profile
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

#### 1. Run the .NET 11 Backend API

Ensure .NET 11 SDK is installed:

```powershell
cd backend
dotnet run --project ChargeLoop.WebApi
```

* **Interactive Swagger UI**: Open [http://localhost:5000](http://localhost:5000) or [http://localhost:5000/swagger](http://localhost:5000/swagger) in your browser.
* **Real-time SignalR Hub**: Available at `http://localhost:5000/hubs/stations`.

---

### 2. Start PostgreSQL + PostGIS and Redis (Optional for local persistent DB)

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
