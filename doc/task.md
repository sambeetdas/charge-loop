# ChargeLoop — Project Tasks & Work Breakdown Structure (WBS)

**Current Status:** Phase 1, Phase 2 Core, Phase 3 Partner MVP, & Phase 4 Testing Complete ✅  
**Last Updated:** November 2026  

---

## 1. Phase 1: Foundation & Core Engine (COMPLETED ✅)

### 1.1 Backend Architecture & API (.NET 11)
- [x] **TASK-B01**: Scaffold .NET 11 Clean Architecture solution (`Domain`, `Application`, `Infrastructure`, `WebApi`).
- [x] **TASK-B02**: Define core Domain entities (`Driver`, `Vehicle`, `Station`, `Partner`, `Battery`, `SwapTransaction`, `WalletTransaction`, `SubscriptionPlan`, `DriverSubscription`, `Reservation`, `SupportTicket`).
- [x] **TASK-B03**: Create Application DTOs and service interfaces (`IStationService`, `ISwapService`, `IWalletService`, `IDriverService`, `ISubscriptionService`, `IReservationService`, `ISupportService`).
- [x] **TASK-B04**: Implement geospatial Haversine station discovery algorithm in `StationService`.
- [x] **TASK-B05**: Implement 2-minute battery swap engine with validation rules (SOC $\ge$ 90%, temperature $< 45^\circ\text{C}$).
- [x] **TASK-B06**: Implement double-entry wallet ledger with automatic driver debit and instant partner commission credit (₹25).
- [x] **TASK-B07**: Implement ASP.NET Core SignalR hub (`/hubs/stations`) for real-time station inventory and swap push.
- [x] **TASK-B08**: Implement REST API Controllers (`AuthController`, `StationsController`, `SwapsController`, `WalletController`, `SubscriptionsController`, `ReservationsController`, `SupportController`).
- [x] **TASK-B09**: Configure Swagger OpenAPI documentation UI at `/swagger`.

### 1.2 Persistent Database (PostgreSQL)
- [x] **TASK-D01**: Verify and connect to local PostgreSQL instance at `localhost:5432/chargeloop`.
- [x] **TASK-D02**: Write and execute DDL schema script (`init_schema.sql`) to create all tables with foreign keys and indexes.
- [x] **TASK-D03**: Implement `ChargeLoopDbContext` using `Npgsql.EntityFrameworkCore.PostgreSQL`.
- [x] **TASK-D04**: Remove all in-memory hardcoding and transition entire stack to live EF Core database queries.
- [x] **TASK-D05**: Populate realistic multi-driver, multi-station, multi-battery seed data across Delhi NCR with zero dummy hardcoded names or phone numbers.
- [x] **TASK-D06**: Verify end-to-end database writes and reads via automated REST probes.

### 1.3 Driver Mobile Client (Flutter)
- [x] **TASK-M01**: Scaffold multiplatform Flutter project structure (`core`, `models`, `screens`, `widgets`).
- [x] **TASK-M02**: Design high-contrast dark electric theme (`AppColors`, `AppTheme`) with 52dp+ touch targets for sunlight readability.
- [x] **TASK-M03**: Build custom circular `BatteryGauge` widget showing live SOC % and visual color thresholds.
- [x] **TASK-M04**: Build `AuthScreen` with mobile OTP login, dynamic phone registration, and English / हिन्दी vernacular switcher.
- [x] **TASK-M05**: Build `StationsMapScreen` with live battery count badges, distance, wait times, and navigation links.
- [x] **TASK-M06**: Build `SwapScreen` with live stopwatch timer, serial handshake (`BAT-48V-OLD` $\rightarrow$ `BAT-48V-NEW`), and GST tax receipt.
- [x] **TASK-M07**: Build `WalletScreen` with live balance, quick UPI AutoPay top-up chips (`+₹200`, `+₹500`, `+₹1000`), and transaction ledger.
- [x] **TASK-M08**: Build `ProfileScreen` with KYC status, linked vehicle details, mounted battery telemetry, welfare cover, and 24/7 SOS helpline.
- [x] **TASK-M09**: Build interactive standalone web simulator served from WebApi `wwwroot/index.html` on `http://localhost:5000` with Driver and Partner modes.

---

## 2. Phase 2: Partner Application & Station Inventory (COMPLETED ✅)

- [x] **TASK-PA01**: Implement Partner station operator selection and view.
- [x] **TASK-PA02**: Build visual Battery Bay Inventory grid (slots showing ready batteries, charging batteries, and empty bays).
- [x] **TASK-PA03**: Build Operator validation workflow for incoming depleted and outgoing charged batteries.
- [x] **TASK-PA04**: Build Partner Commission wallet display with instant ₹25.00 credit per swap and UPI withdrawal.
- [x] **TASK-PA05**: Build Station Status toggle (Open / Closed / Maintenance) updating PostgreSQL in real time.
- [x] **TASK-PA06**: Build Hardware & Charger defect ticketing system writing directly to `support_tickets` in PostgreSQL.

---

## 3. Phase 3: Automated Testing & Verification (COMPLETED ✅)

- [x] **TASK-T01**: Create `ChargeLoop.UnitTests` project targeting `net11.0` using xUnit and EF Core InMemory.
- [x] **TASK-T02**: Write unit tests for `SwapService` (fee calculation ₹100 + ₹18 GST = ₹118, ₹25 partner commission, SOC $< 90\%$ rejection, battery reassignment).
- [x] **TASK-T03**: Write unit tests for `WalletService` (double-entry atomic credit, insufficient balance handling, partner instant credit).
- [x] **TASK-T04**: Execute automated test suite with 100% pass rate (`6/6 tests passed`).

---

## 4. Phase 4: Production Integrations & Expansion (PENDING / ROADMAP 🟡)

- [ ] **TASK-H01**: Integrate `mobile_scanner` plugin in Flutter to scan physical QR codes and battery barcodes using native camera.
- [ ] **TASK-H02**: Integrate `google_maps_flutter` with Google Maps API key for live map tile rendering and marker clustering.
- [ ] **TASK-H03**: Implement GPS location permissions (`geolocator`) to sort stations by real GPS coordinates.
- [ ] **TASK-S01**: Integrate Indian SMS OTP service (Gupshup / MSG91) with DLT registration templates.
- [ ] **TASK-P01**: Integrate Razorpay / Cashfree UPI Intent SDK for in-app UPI payments.
- [ ] **TASK-A01**: Configure iOS runner (`ios/`) with Apple Sign-in and iOS location entitlements.
- [ ] **TASK-A02**: Configure macOS desktop target (`macos/`) for station partners and operations managers with receipt printer drivers.
