# ChargeLoop — Project Tasks & Work Breakdown Structure (WBS)

**Current Status:** Phase 1 Complete | Phase 2 In Progress  
**Last Updated:** November 2026  

---

## 1. Phase 1: Foundation & Core Engine (COMPLETED ✅)

### 1.1 Backend Architecture & API (.NET 11)
- [x] **TASK-B01**: Scaffold .NET 11 Clean Architecture solution (`Domain`, `Application`, `Infrastructure`, `WebApi`).
- [x] **TASK-B02**: Define core Domain entities (`Driver`, `Vehicle`, `Station`, `Partner`, `Battery`, `SwapTransaction`, `WalletTransaction`).
- [x] **TASK-B03**: Create Application DTOs and service interfaces (`IStationService`, `ISwapService`, `IWalletService`, `IDriverService`).
- [x] **TASK-B04**: Implement geospatial Haversine station discovery algorithm in `StationService`.
- [x] **TASK-B05**: Implement 2-minute battery swap engine with validation rules (SOC $\ge$ 90%, temperature $< 45^\circ\text{C}$).
- [x] **TASK-B06**: Implement double-entry wallet ledger with automatic driver debit and instant partner commission credit (₹25).
- [x] **TASK-B07**: Implement ASP.NET Core SignalR hub (`/hubs/stations`) for real-time station inventory and swap push.
- [x] **TASK-B08**: Implement REST API Controllers (`AuthController`, `StationsController`, `SwapsController`, `WalletController`, `TelemetryController`).
- [x] **TASK-B09**: Configure Swagger OpenAPI documentation UI at `/swagger`.

### 1.2 Persistent Database (PostgreSQL)
- [x] **TASK-D01**: Verify and connect to local PostgreSQL instance at `localhost:5432/chargeloop`.
- [x] **TASK-D02**: Write and execute DDL schema script (`init_schema.sql`) to create all tables with foreign keys and indexes.
- [x] **TASK-D03**: Implement `ChargeLoopDbContext` using `Npgsql.EntityFrameworkCore.PostgreSQL`.
- [x] **TASK-D04**: Remove all in-memory hardcoding and transition service layer to async EF Core database queries.
- [x] **TASK-D05**: Populate initial real-world seed data (Delhi NCR swap hubs, test batteries, test driver, test partner).
- [x] **TASK-D06**: Verify end-to-end database writes and reads via automated REST probe.

### 1.3 Driver Mobile Client (Flutter)
- [x] **TASK-M01**: Scaffold multiplatform Flutter project structure (`core`, `models`, `screens`, `widgets`).
- [x] **TASK-M02**: Design high-contrast dark electric theme (`AppColors`, `AppTheme`) with 52dp+ touch targets for sunlight readability.
- [x] **TASK-M03**: Build custom circular `BatteryGauge` widget showing live SOC % and visual color thresholds.
- [x] **TASK-M04**: Build `AuthScreen` with mobile OTP login and English / हिन्दी vernacular switcher.
- [x] **TASK-M05**: Build `StationsMapScreen` with live battery count badges, distance, wait times, and navigation links.
- [x] **TASK-M06**: Build `SwapScreen` with live stopwatch timer, serial handshake (`BAT-48V-OLD` $\rightarrow$ `BAT-48V-NEW`), and GST tax receipt.
- [x] **TASK-M07**: Build `WalletScreen` with live balance, quick UPI AutoPay top-up chips (`+₹100`, `+₹250`, `+₹500`), and transaction ledger.
- [x] **TASK-M08**: Build `ProfileScreen` with KYC status, linked vehicle details (`Quantum Energy Plaza EV 2W`), welfare cover, and 24/7 SOS helpline.
- [x] **TASK-M09**: Build interactive standalone web client served from WebApi `wwwroot/index.html` on `http://localhost:5000`.

---

## 2. Phase 2: Hardware Bindings & Production Hardening (IN PROGRESS 🟡)

### 2.1 Mobile Hardware Integration
- [ ] **TASK-H01**: Integrate `mobile_scanner` plugin in Flutter to scan physical QR codes and battery barcodes using native camera.
- [ ] **TASK-H02**: Integrate `google_maps_flutter` with Google Maps API key for live map tile rendering and marker clustering.
- [ ] **TASK-H03**: Implement GPS location permissions (`geolocator`) to sort stations by real GPS coordinates.

### 2.2 Security & Authentication
- [ ] **TASK-S01**: Implement ASP.NET Core JWT Bearer authentication middleware with token refresh.
- [ ] **TASK-S02**: Integrate Indian SMS OTP service (Gupshup / MSG91) with DLT registration templates.
- [ ] **TASK-S03**: Implement Request Idempotency filter (`X-Idempotency-Key`) for payment and swap endpoints.

### 2.3 Payment Gateway
- [ ] **TASK-P01**: Integrate Razorpay / Cashfree UPI Intent SDK into mobile wallet screen.
- [ ] **TASK-P02**: Implement webhook listener in WebApi with cryptographic signature verification for automated payment reconciliation.

---

## 3. Phase 3: Partner Application MVP (PENDING 🔴)

- [ ] **TASK-PA01**: Create Partner App entry point and operator sign-in.
- [ ] **TASK-PA02**: Build Battery Bay Inventory grid (visual slots showing charging batteries, fully charged ready packs, and maintenance bays).
- [ ] **TASK-PA03**: Build Operator Barcode Scanner for scanning incoming depleted battery and outgoing charged battery.
- [ ] **TASK-PA04**: Build Partner Earnings screen with daily/monthly swap volume, commission statements, and 1-click UPI bank payout.
- [ ] **TASK-PA05**: Build Station Status toggle (Open / Closed / Maintenance / Power Outage).

---

## 4. Phase 4: Automated Testing & CI/CD (PENDING 🔴)

- [ ] **TASK-T01**: Create `ChargeLoop.UnitTests` project using xUnit and FluentAssertions.
- [ ] **TASK-T02**: Write unit tests for `SwapService` (boundary conditions: battery SOC $< 90\%$, cell temp $> 45^\circ\text{C}$, insufficient wallet balance).
- [ ] **TASK-T03**: Write unit tests for `WalletService` (double-entry accounting reconciliation, zero balance variance).
- [ ] **TASK-T04**: Write integration tests using PostgreSQL database fixtures.
- [ ] **TASK-T05**: Setup GitHub Actions workflow for automated build, test execution, and Docker packaging.

---

## 5. Phase 5: Apple Ecosystem & Expansion (PENDING 🔴)

- [ ] **TASK-A01**: Configure iOS runner (`ios/`) with Apple Sign-in and iOS location entitlements.
- [ ] **TASK-A02**: Configure macOS desktop target (`macos/`) for station partners and operations managers with receipt printer drivers.
- [ ] **TASK-A03**: Test and publish to Apple App Store (TestFlight) and Google Play Store.
