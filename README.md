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

1. **Partner Sign Up / Sign In / Reset PIN**:
   - **Sign Up**: Select **Partner / Vendor**, enter mobile number, **PIN** (4-6 digits), and **Confirm PIN** (plus optional Full Name and Business / Hub Name) (`POST /api/v1/auth/vendor/signup`).
   - **Sign In**: Enter mobile number and **PIN** (`POST /api/v1/auth/vendor/signin`) to receive a JWT token.
   - **Reset PIN**: Use **Reset PIN** with mobile number, **New PIN**, and **Confirm New PIN** (`POST /api/v1/auth/vendor/reset-pin`).
   - Initial Partner Commission Wallet starts at **₹0.00** (same phone number can be registered as both Driver and Partner with separate profiles and wallets).
2. **Store & Battery Inventory Management (4-Status Lifecycle)**:
   - View **📥 Active Swap Requests (Your Stores)** (`GET /api/v1/swaps/partner/{partnerId}/pending`) and click **Go to Swap Desk →** to jump directly to Swap Desk with the driver & store pre-selected.
   - Add one or more Stations/Hubs (`POST /api/v1/stations`) using **📍 Find** address geocoding (`GET /api/v1/config/geocode`, Google Maps API with Nominatim fallback). New stores start with **0** batteries.
   - Register individual batteries at a store — Serial/QR, DB-driven Battery Type (`GET /api/v1/config/battery-types`), and SOC % (`POST /api/v1/stations/{stationId}/batteries`). Batteries with SOC ≥ 80% start as **Active**; batteries with SOC < 80% start as **Charging**.
   - View **all** store batteries (`GET /api/v1/stations/{stationId}/batteries?includeAll=true`).
   - Change any battery's status with the 4-way toggle (`PATCH /api/v1/stations/{stationId}/batteries/{batteryId}/status`):
     - **Active** (`AvailableCharged = 0`): Visible to drivers and eligible for reservation/assignment.
     - **Inactive** (`Inactive = 4`): Hidden from drivers and cannot be assigned.
     - **Charging** (`ChargingInStation = 1`): Hidden from drivers and cannot be assigned while charging.
     - **Out of Order** (`MaintenanceFaulty = 3`): Hidden from drivers and blocked from assignment.
   - **Rule**: Only **Active** batteries can be shown to drivers or assigned during a swap.
3. **Partner Profile & Phone Number Change (PIN)**:
   - Edit Full Name, Business Name, Email, UPI ID, and Bank Account Number (`PUT /api/v1/auth/vendor/profile/{vendorId}`).
   - Change phone number securely using your current PIN (`POST /api/v1/auth/vendor/profile/{vendorId}/change-phone`).
   - Access the in-app **❓ Help** tab anytime for a complete Partner workflow guide.
4. **Step 1 — Validate Driver Before Swap**:
   - Enter Driver phone, name, or ID and click **Validate** (`GET /api/v1/swaps/validate-driver?query=...`).
5. **Step 2 — 2-Step Battery QR Scan**:
   - Scan/enter the Driver's **Depleted Battery QR** (automatically prefilled if the driver already holds a battery, or left **empty** if it is the driver's first battery pickup) and select an **Active** charged battery from the store's inventory.
6. **Step 3 — Verify Driver's Swap OTP**:
   - Enter the Driver's **6-digit Swap OTP** (from the Driver's Scan & Pay swap request) and click **Verify OTP** (`GET /api/v1/swaps/validate-otp?otpCode=...&driverId=...`).
7. **Step 4 — Collect Payment & Execute Swap**:
   - On OTP validation, **Cash Collected (₹)** (`₹118.00`) is displayed with the complete **Fare Breakup** (`Base Fee ₹100.00 + GST 18% ₹18.00 = ₹118.00`; future online payment transaction details will also appear here).
   - Click **Confirm Swap & Collect Payment** (`POST /api/v1/swaps/confirm`) to complete the Driver's swap request.
   - Partner commission (**₹25.00**) is credited instantly to the Partner Commission Wallet and logged in `transactions` + `audit_logs`.
   - Any active reservation for that driver at the station is automatically marked `Fulfilled`, and the returned depleted battery (if any) moves to `Charging` at the store.
8. **Step 5 — Publish Bill**:
   - Dispatch/record the GST invoice to the Driver across **WhatsApp**, **SMS**, and **Email** (`POST /api/v1/swaps/{swapId}/publish-bill`).

---

### 🚗 Driver Flow

1. **Driver Sign Up / Sign In / Reset PIN**:
   - **Sign Up**: Select **Driver**, enter mobile number, **PIN** (4-6 digits), and **Confirm PIN** (plus optional Name, Email for invoices, and Referral Code) (`POST /api/v1/auth/signup`).
   - **Sign In**: Enter mobile number and **PIN** (`POST /api/v1/auth/signin`) to receive a JWT token.
   - **Reset PIN**: Use **Reset PIN** with mobile number, **New PIN**, and **Confirm New PIN** (`POST /api/v1/auth/reset-pin`).
   - Initial Driver Wallet starts at **₹0.00**.
2. **Station Locator, Pre-Reserve Battery Preview & 2-Hour Reservation**:
   - Browse nearby swap hubs (`POST /api/v1/stations/nearby`) with live **Active (Charged)** and **Charging** counts.
   - Tap **🔋 Batteries** on any station card to preview **Active** batteries at that station **before** reserving (`GET /api/v1/stations/{stationId}/batteries`).
   - Tap **Reserve** (`POST /api/v1/reservations/hold`) to hold an Active battery for **2 hours** with a live countdown (no OTP on Reserve). After **2 hours**, the reservation automatically expires so you can reserve again; you can also tap **Reverse Reserve** (`DELETE /api/v1/reservations/{reservationId}`) at any time.
3. **Scan & Pay (Swap Request + Swap OTP & Itemized Fare Breakup)**:
   - In **Scan Station & Battery QR**, only your active **Reserved Station** is shown.
   - **First Battery vs Replacement**:
     - First battery pickup: the depleted battery field stays **empty**.
     - Replacement swap: the depleted battery field automatically displays your currently assigned battery QR/Serial.
   - Select an Active battery to receive, review the **read-only** Cash amount with its complete DB-driven **Fare Breakup** (`GET /api/v1/config/pricing`: Base Swap Fee + **Platform Fee (% of Base Swap Fee)** + **Convenience Fee** + **Payment Gateway Fee** *(₹0 for Cash)* + **Bank Charges** *(₹0 for Cash)* + **GST %**), and tap **Confirm Swap & Record Payment** (`POST /api/v1/swaps/initiate`).
   - This submits a **Swap Request** (Pending Vendor Confirmation) and generates a **6-digit Swap OTP** on screen. You can tap **🔄 Regenerate OTP** (`POST /api/v1/swaps/{swapId}/regenerate-otp`) at any time before the vendor confirms.
   - Once the Vendor verifies your Swap OTP and clicks **Confirm Swap**, your request is marked **Completed**.
4. **Wallet, Cash Top-Up & Referral**:
   - View wallet balance and recent transactions (`GET /api/v1/wallet/balance/{driverId}?accountType=0`), record a **Cash Top-Up** (`POST /api/v1/wallet/topup` with `CorrelationId` idempotency), or claim a friend's `CL-...` referral code (`POST /api/v1/auth/referral/claim`) for **₹50** bonus.
5. **Billing & Swap History**:
   - View GST invoices (`INV-CL-YYYYMMDD-XXXXXX`), tax breakdown, payment mode, and published channels (`GET /api/v1/swaps/history/{driverId}`).
6. **Profile, KYC, Help & Phone Number Change (PIN)**:
   - Edit Full Name, Email, and Language (`PUT /api/v1/auth/profile/{driverId}`), complete KYC (`POST /api/v1/auth/kyc`), subscribe to Weekly Plans, change your phone number using your current PIN (`POST /api/v1/auth/profile/{driverId}/change-phone`), and view the in-app **❓ Help** tab.

> [!WARNING]
> **Backlog Items**:
> - **Online Payments (Card / UPI / Wallet Checkout)**: Online payment gateway integration is currently in progress. Selecting **Wallet**, **Card**, or **UPI** in the Web or Mobile UI displays: *"Work in progress to integrate online payment. Please wait for few days."*
> - **Passwordless & SMS OTP Authentication**: Legacy passwordless (`/auth/phone`, `/auth/vendor/phone`) and demo OTP (`/auth/verify-otp`, `/auth/vendor/verify-otp`) login endpoints have been removed in favor of PIN-based auth (`signup` / `signin` / `reset-pin`) and are kept in the backlog for future SMS gateway (MSG91 / Twilio) integration.

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for full details.

Copyright (c) 2026 ChargeLoop Energy Solutions.
