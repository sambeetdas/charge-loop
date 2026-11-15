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

## 4. Phase 4: Partner & Driver Workflow Enhancements (PLANNED 📋)

### 4.1 Partner (Vendor) Workflow Tasks
- [x] **TASK-PA07 (Driver Validation before Swap)**: Add `GET /api/v1/swaps/validate-driver?query=...` endpoint and Partner Swap Desk UI to validate Driver KYC, active vehicle, currently mounted battery, subscription pass, and wallet balance before unlocking the Swap button.
- [x] **TASK-PA08 (Partner 2-Step Battery QR Scan)**: Add Partner-driven Swap workflow: (1) Scan Driver's old battery QR code for disconnection, and (2) Scan new charged station battery QR code for assignment to the driver.
- [x] **TASK-PA09 (Cash-First Payment & WIP Online Gate)**: Support **Cash** (with manual Cash amount input), **Wallet**, and **Weekly Subscription Quota**, while showing *"Work in progress to integrate online payment. Please wait for few days."* for Card/UPI.
- [x] **TASK-PA10 (Register Battery with Store Details)**: Add `POST /api/v1/stations/{stationId}/batteries` endpoint and Partner UI form to register individual battery packs (Serial Number, Model Type, SOC %, SOH %) linked to a specific Store/Station.
- [x] **TASK-PA11 (Multi-Channel Bill Publishing)**: Generate GST Invoice numbers (`INV-CL-YYYYMMDD-XXXXXX`) and implement `POST /api/v1/swaps/{swapId}/publish-bill` to record bill dispatch channels.

### 4.2 Driver Workflow Tasks
- [x] **TASK-DR01 (Phone Registration with OTP Verification)**: Implement 2-step OTP flow (`POST /api/v1/auth/send-otp` and `POST /api/v1/auth/verify-otp`) with 6-digit demo OTP shown in UI.
- [x] **TASK-DR02 (Billing History & Invoice Viewer)**: Upgrade Driver Swap History into a full Billing History view with invoice numbers, tax breakdown, payment mode badges, and viewable bills.
- [x] **TASK-DR03 (Scan & Pay + Cash Input)**: Add QR Scan & Pay workflow with Cash amount input and online payment WIP notice.
- [x] **TASK-DR04 (Refer a Friend Program)**: Add unique referral code generation per driver (`referral_code`, `referred_by_code` in `drivers` table), `POST /api/v1/auth/referral/claim` endpoint, and "Refer & Earn ₹50" UI card in Web & Mobile.

---

## 5. Phase 5: Production Hardening & Backlog

### 5.1 Completed Security, Ledger & Mobile Parity
- [x] **TASK-SEC01 (JWT Authentication)**: Issue signed HMAC-SHA256 JWT tokens on Driver & Partner sign-in, enforce `JwtBearer` authentication in `Program.cs`, and attach `Authorization: Bearer <token>` in Web & Flutter clients.
- [x] **TASK-LEDGER01 (Unified `transactions` Table + Idempotency)**: Create `transactions` table in PostgreSQL keyed by unique `correlation_id` (`Guid`) so every Cash swap & Cash top-up is idempotently recorded.
- [x] **TASK-AUDIT01 (End-to-End `audit_logs` Table)**: Add `CorrelationAndAuditMiddleware` (`X-Correlation-Id` `Guid`) and `audit_logs` table tracking every `/api/*` request, duration, actor, and domain event.
- [x] **TASK-MOB01 (Flutter Mobile Parity)**: Wire 2-step OTP login into `auth_screen.dart`, Cash top-up + online WIP notice into `wallet_screen.dart`, and Partner Swap Desk + Register Battery into `vendor_home_screen.dart`.

### 5.2 Backlog (Next Integrations)
- [ ] **TASK-B01 (Real OTP SMS Gateway)**: Replace demo OTP UI display with actual SMS delivery via MSG91 / Gupshup / Twilio (DLT templates).
- [ ] **TASK-B02 (Online Payment Gateway)**: Integrate Razorpay / Cashfree / Stripe for Card, UPI Intent, and online Wallet top-ups with webhook signature verification.
- [ ] **TASK-B03 (Real Bill Dispatch)**: Connect `PublishSwapBillAsync` to WhatsApp Business API, SMS, and SMTP/SendGrid email delivery.
- [ ] **TASK-H01**: Integrate `mobile_scanner` plugin in Flutter to scan physical QR codes and battery barcodes using native camera.
- [ ] **TASK-H02**: Integrate `google_maps_flutter` with Google Maps API key for live map tile rendering and marker clustering.
- [ ] **TASK-H03**: Implement GPS location permissions (`geolocator`) to sort stations by real GPS coordinates.
