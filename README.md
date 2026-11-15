# ChargeLoop EV Battery Swapping Application Suite

ChargeLoop operates an enterprise Battery-as-a-Service (BaaS) swapping network for electric 2-wheelers and 3-wheelers. Drivers exchange a depleted battery for a fully charged one quickly at partner-run stations.

---

## 🏗️ System Architecture

* **Mobile App Client**: **Flutter (Dart)** — Multiplatform codebase designed for **Android first** (budget device friendly, high-contrast sunlight readability) and seamlessly deployable to **iOS & macOS**.
* **Backend Core API**: **ASP.NET Core (.NET 11, C#)** — High-throughput Clean Architecture API with:
  * **Real-time SignalR Hub** (`/hubs/stations`) for instant battery count and swap state broadcasting.
  * **Swap Engine** for fast battery validation and physical swap verification.
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
│       └── screens/                      # Auth, Stations Map, Battery Swap, Wallet, Vendor Operations & Profiles
└── docker-compose.yml                    # PostgreSQL + PostGIS & Redis services
```

---

## 🚀 Getting Started

### ⚡ 1-Click Launch (Backend + Mobile App)

Simply run the PowerShell script:

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

## 🔄 End-to-End User Flows

> [!IMPORTANT]
> **Security & Idempotency**: All protected endpoints require a signed **JWT Bearer token** (`Authorization: Bearer <token>`), and every payment/wallet operation is tracked end-to-end in PostgreSQL (`transactions` and `audit_logs`) using a unique `X-Correlation-Id` (`Guid`) for idempotency.

> [!NOTE]
> **Demo OTP Mode**: During the MVP phase, `POST /api/v1/auth/send-otp` displays the 6-digit OTP directly in the UI for both Partner/Vendor and Driver sign-in and phone-number updates. Real SMS gateway delivery (MSG91 / Twilio) is tracked in the backlog.

### 🏪 Partner (Vendor) Flow

> [!TIP]
> **Multi-Store Support**: A single Partner can operate multiple stations/hubs. Sign in first, then add stores, register batteries, and control which charged batteries are visible to drivers from the **Stores** tab.

1. **Partner Sign In / Sign Up (2-Step OTP)**:
   - Select **Partner / Vendor**, enter mobile number, and tap **Send OTP to Phone** (`POST /api/v1/auth/send-otp`).
   - Enter the 6-digit OTP (plus optional Full Name and Business / Hub Name) and tap **Verify & Sign In** (`POST /api/v1/auth/vendor/verify-otp`) to receive a JWT token.
   - Initial Partner Commission Wallet starts at **₹0.00**.
2. **Store & Battery Inventory Management**:
   - Add a new Station/Hub (`POST /api/v1/stations`).
   - Register individual batteries at a store — Serial/QR, Model Type, and SOC % (`POST /api/v1/stations/{stationId}/batteries`).
   - View **all** batteries in the store (`GET /api/v1/stations/{stationId}/batteries?includeAll=true`), including both `Charged` (SOC ≥ 80%) and `Charging` batteries.
   - Toggle **Shown / Hidden from Driver** (`PATCH /api/v1/stations/{stationId}/batteries/{batteryId}/visibility`) on any charged battery. Undercharging (`Charging`) batteries stay hidden from drivers automatically.
3. **Partner Profile & Phone Number Change (OTP)**:
   - Edit Full Name, Business Name, Email, UPI ID, and Bank Account Number (`PUT /api/v1/auth/vendor/profile/{vendorId}`).
   - Change phone number securely with 2-step OTP verification (`POST /api/v1/auth/send-otp` &rarr; `POST /api/v1/auth/vendor/profile/{vendorId}/change-phone`).
4. **Step 1 — Validate Driver Before Swap**:
   - Enter Driver phone, name, or ID and click **Validate** (`GET /api/v1/swaps/validate-driver?query=...`).
   - The **Swap** button stays locked until the driver's KYC, active battery, wallet balance, and Weekly Pass quota are verified.
5. **Step 2 — 2-Step Battery QR Scan**:
   - Scan/enter the Driver's **Depleted Battery QR** (or leave empty if the driver is picking up their first battery) and select the **New Charged Battery QR** from the store's inventory.
6. **Step 3 — Collect Payment & Execute Swap**:
   - Collect **Cash** (`POST /api/v1/swaps/initiate` &rarr; `POST /api/v1/swaps/confirm`). Wallet, Card, and UPI show a Work-In-Progress notice.
   - Partner commission (**₹25.00**) is credited instantly and logged in `transactions`.
7. **Step 4 — Publish Bill**:
   - Dispatch/record the GST invoice to the Driver across **WhatsApp**, **SMS**, and **Email** (`POST /api/v1/swaps/{swapId}/publish-bill`).

---

### 🚗 Driver Flow

1. **Phone Registration & OTP Verification**:
   - Select **Driver** on the Sign In / Sign Up screen, enter a mobile number, and tap **Send OTP to Phone** (`POST /api/v1/auth/send-otp`).
   - Enter the 6-digit OTP (plus optional Name, Email for invoices, and Referral Code) and tap **Verify & Sign In** (`POST /api/v1/auth/verify-otp`) to receive a JWT token.
   - Initial Driver Wallet starts at **₹0.00**.
2. **Station Locator, Pre-Reserve Battery Preview & 2-Hour Reservation OTP**:
   - Browse nearby swap hubs (`POST /api/v1/stations/nearby`) with live **Charged** and **Charging** counts.
   - Tap **🔋 Batteries** on any station card to preview available charged batteries visible to drivers **before** reserving (`GET /api/v1/stations/{stationId}/batteries`).
   - Tap **Reserve** (`POST /api/v1/reservations/hold`) to receive a **6-digit Reservation OTP** displayed on screen for **2 hours** (automatically disappears after 2 hours so you can reserve again), or tap **Reverse / Cancel Reserve** (`DELETE /api/v1/reservations/{reservationId}`) at any time.
3. **Scan & Pay (Reserved Station Only)**:
   - In **Scan Station & Battery QR**, only your **Reserved Station** is shown.
   - **First Battery vs Replacement**: If this is your first battery pickup, the depleted battery field stays **empty**; if you already have an assigned battery and are coming for a replacement, it automatically displays your assigned battery QR/Serial.
   - Select the charged battery, pay via **Cash** (`POST /api/v1/swaps/initiate` &rarr; `POST /api/v1/swaps/confirm`), and view your GST receipt.
4. **Wallet, Cash Top-Up & Referral**:
   - View wallet balance and recent transactions (`GET /api/v1/wallet/balance/{driverId}?accountType=0`), record a **Cash Top-Up** (`POST /api/v1/wallet/topup` with `CorrelationId` idempotency), or claim a friend's `CL-...` referral code (`POST /api/v1/auth/referral/claim`) for **₹50** bonus.
5. **Billing & Swap History**:
   - View GST invoices (`INV-CL-YYYYMMDD-XXXXXX`), tax breakdown, payment mode, and published channels (`GET /api/v1/swaps/history/{driverId}`).
6. **Profile, KYC & Phone Number Change (OTP)**:
   - Edit Full Name, Email, and Language (`PUT /api/v1/auth/profile/{driverId}`), complete KYC (`POST /api/v1/auth/kyc`), subscribe to Weekly Plans, and change your phone number via 2-step OTP (`POST /api/v1/auth/send-otp` &rarr; `POST /api/v1/auth/profile/{driverId}/change-phone`).

> [!WARNING]
> **Online Payments (Card / UPI / Wallet Checkout)**: Online payment gateway integration is currently in progress. Selecting **Wallet**, **Card**, or **UPI** in the Web or Mobile UI displays: *"Work in progress to integrate online payment. Please wait for few days."*

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for full details.

Copyright (c) 2026 ChargeLoop Energy Solutions.
