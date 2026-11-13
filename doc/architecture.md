# ChargeLoop — Technical Architecture Blueprint

**Platform:** .NET 11 (Core Backend) | PostgreSQL (Database) | Flutter (Cross-Platform Mobile)  
**Target Environments:** Android (Primary), Apple iOS & macOS (Expansion Ready)  

---

## 1. System Topology Overview

```mermaid
flowchart TD
    subgraph Clients["Client Layer"]
        DA["Driver Mobile App (Flutter: Android / iOS)"]
        PA["Partner Mobile App (Flutter: Android / macOS)"]
        WebAdmin["Admin & Operations Console (Web)"]
    end

    subgraph API["Backend Service Layer (ASP.NET Core .NET 11)"]
        Gateway["Reverse Proxy & Rate Limiter (Kestrel / YARP)"]
        Hub["SignalR Hub (/hubs/stations) - Live Real-Time Push"]
        
        subgraph Controllers["WebApi REST Controllers"]
            AuthCtrl["AuthController (OTP, KYC, Vehicle Link)"]
            StationCtrl["StationsController (Nearby, Availability)"]
            SwapCtrl["SwapsController (Initiate, Confirm, History)"]
            WalletCtrl["WalletController (Balance, UPI Top-Up)"]
            TelemCtrl["TelemetryController (SOC, Temp, GPS)"]
        end

        subgraph CoreServices["Domain & Application Services"]
            StationSvc["StationService (Haversine Spatial Search)"]
            SwapSvc["SwapService (Validation & 2-Min Engine)"]
            WalletSvc["WalletService (Double-entry Ledger)"]
            DriverSvc["DriverService (Identity & Verification)"]
        end
    end

    subgraph DataTier["Data & Cache Tier"]
        PG[("PostgreSQL Database (Localhost / RDS)\nTables: drivers, vehicles, stations, batteries, swaps, wallets")]
        Redis[("Redis 7 (In-Memory Cache & Distributed Lock)")]
    end

    DA -->|HTTPS REST| Gateway
    PA -->|HTTPS REST| Gateway
    WebAdmin -->|HTTPS REST| Gateway
    
    DA -.->|WebSocket SignalR| Hub
    PA -.->|WebSocket SignalR| Hub

    Gateway --> Controllers
    Controllers --> CoreServices
    CoreServices -->|Entity Framework Core (Npgsql)| PG
    CoreServices -.->|Cache & Lock| Redis
```

---

## 2. Backend Clean Architecture (.NET 11)

The backend is organized into four decoupled layers following Clean Architecture and Domain-Driven Design (DDD):

```
backend/
├── ChargeLoop.slnx
├── ChargeLoop.Domain/            # Enterprise Business Rules & Entities
│   ├── Entities/                 # Driver, Vehicle, Station, Partner, Battery, SwapTransaction, WalletTransaction
│   └── Enums/                    # KycStatus, StationStatus, BatteryStatus, SwapStatus, PaymentMethod
├── ChargeLoop.Application/       # Application Business Rules
│   ├── DTOs/                     # Strongly-typed records (StationDto, SwapResponse, WalletTopUpRequest)
│   └── Interfaces/               # Service contracts (IStationService, ISwapService, IWalletService, IDriverService)
├── ChargeLoop.Infrastructure/    # External Concerns & Data Access
│   ├── Persistence/              # ChargeLoopDbContext (EF Core Npgsql), Schema DDL
│   ├── Services/                 # Concrete service implementations querying PostgreSQL
│   └── DependencyInjection.cs    # Service registration and DbContext lifecycle setup
└── ChargeLoop.WebApi/            # Interface Adapters & HTTP Pipeline
    ├── Controllers/              # RESTful API controllers
    ├── Hubs/                     # StationHub (SignalR bi-directional push)
    ├── wwwroot/                  # Web app static assets
    └── Program.cs                # Kestrel configuration, CORS, and middleware pipeline
```

### Key Architectural Traits
1. **Zero Hardcoded Data**: All queries in `StationService`, `DriverService`, `SwapService`, and `WalletService` execute against PostgreSQL via `ChargeLoopDbContext`.
2. **Sub-1.5s Swap Latency**: Highly optimized asynchronous queries (`AsNoTracking()`, selective projection) ensure rapid validation during physical battery swaps.
3. **Double-Entry Financial Safety**: Wallet operations are atomic. Every driver debit has an immutable audit log, and partner commission credits (₹25) are written in the same transaction boundary.

---

## 3. Database Schema Design (PostgreSQL)

The database runs on PostgreSQL at `localhost:5432/chargeloop`.

```mermaid
erDiagram
    PARTNERS ||--o{ STATIONS : owns
    PARTNERS ||--o{ SWAP_TRANSACTIONS : earns_from
    DRIVERS ||--o{ VEHICLES : owns
    DRIVERS ||--o{ SWAP_TRANSACTIONS : initiates
    STATIONS ||--o{ BATTERIES : stores
    STATIONS ||--o{ SWAP_TRANSACTIONS : hosts
    BATTERIES ||--o{ SWAP_TRANSACTIONS : returned_issued

    PARTNERS {
        uuid id PK
        varchar full_name
        varchar business_name
        varchar phone_number UK
        varchar upi_id
        numeric wallet_balance
    }

    STATIONS {
        uuid id PK
        uuid partner_id FK
        varchar name
        text address
        varchar city
        double_precision latitude
        double_precision longitude
        int available_charged_batteries
        int charging_batteries
        int estimated_wait_minutes
    }

    DRIVERS {
        uuid id PK
        varchar phone_number UK
        varchar full_name
        varchar language_code
        int kyc_status
        varchar driving_license_number
        varchar aadhaar_masked
        numeric wallet_balance
    }

    VEHICLES {
        uuid id PK
        uuid driver_id FK
        varchar registration_number UK
        varchar oem
        varchar model
        varchar battery_compatibility_type
    }

    BATTERIES {
        uuid id PK
        varchar serial_number UK
        varchar model_type
        int state_of_charge_percent
        int state_of_health_percent
        double_precision cell_temperature_celsius
        int cycle_count
        int status
        uuid current_station_id FK
        uuid current_driver_id FK
    }

    SWAP_TRANSACTIONS {
        uuid id PK
        uuid driver_id FK
        uuid station_id FK
        uuid partner_id FK
        uuid depleted_battery_id FK
        uuid issued_battery_id FK
        int depleted_battery_soc
        int issued_battery_soc
        numeric base_fee
        numeric tax_amount
        numeric total_amount
        numeric partner_commission_earned
        int status
        int duration_seconds
        timestamptz initiated_at
        timestamptz completed_at
    }

    WALLET_TRANSACTIONS {
        uuid id PK
        int account_type
        uuid account_id
        numeric amount
        int type
        varchar reason
        varchar reference_id
        timestamptz timestamp
    }
```

---

## 4. The 2-Minute Physical Swap Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Driver as Driver (Mobile)
    actor Partner as Station Operator
    participant API as .NET 11 WebApi
    participant DB as PostgreSQL

    Driver->>Partner: Arrives at station with depleted battery (16% SOC)
    Driver->>Driver: Opens Swap screen & presents Dynamic QR
    Partner->>API: POST /api/v1/swaps/initiate (DriverID, DepletedSerial, IssuedSerial)
    
    activate API
    API->>DB: Query Station, Partner, and Battery Serials
    API->>DB: Validate Issued Battery SOC >= 90% & Temp < 45°C
    API->>DB: Insert new SwapTransaction (Status: Validated)
    API-->>Partner: Handshake Validated (< 1.5 seconds)
    API-->>Driver: Push Swap Initiated Notification
    deactivate API

    Note over Driver,Partner: Physical battery exchange into vehicle (~60–90 sec)

    Partner->>API: POST /api/v1/swaps/confirm (SwapID, PaymentMethod: Wallet)
    activate API
    API->>DB: Deduct Driver Wallet (₹118 including 18% GST)
    API->>DB: Credit Partner Commission (₹25)
    API->>DB: Reassign Batteries (Depleted -> Charging, Issued -> Driver)
    API->>DB: Update Station Count (Available -1, Charging +1)
    API->>DB: Update Swap Status -> Completed (Duration Recorded)
    API-->>Partner: Payout Credited Instantly
    API-->>Driver: Digital Tax Receipt (100% Full SOC)
    deactivate API
```

---

## 5. Mobile Application Client Architecture (Flutter)

* **Multiplatform Portability**: Single Dart codebase compiling directly to native ARM machine code on Android, iOS, and macOS desktop without bridging penalties.
* **Sunlight-Optimized Theme**:
  * Dark slate backgrounds (`#0F141A`, `#1A222C`) with high-contrast electric accents (Neon Green `#00E676`, Cyan `#00E5FF`, Safety Amber `#FFB300`).
  * Minimum **52dp** touch targets tailored for gig riders operating on motorbikes with gloves.
* **Network & Offline Caching**:
  * Uses `http` with timeout interceptors.
  * Local caching ensures that drivers can view saved station bookmarks and history even if 3G connectivity drops while driving through low-network pockets.
* **State Management**: Reactive StateFlow / Provider pattern separating presentation widgets from business logic and REST/SignalR services.

---

## 6. Real-Time Communication with SignalR

Instead of having thousands of mobile devices constantly polling the server every few seconds (which drains battery and overloads APIs), ASP.NET Core SignalR provides event-driven pushes:

* **Hub Route**: `/hubs/stations`
* **Events**:
  * `ReceiveStationUpdate(StationDto)`: Dispatched to all drivers whenever battery stock changes at any station.
  * `StationInventoryChanged(StationDto)`: Scoped to drivers currently navigating to a specific station group (`Station_{id}`).
  * `ReceiveSwapStatus(SwapResponse)`: Dispatched directly to the driver's private session (`Driver_{id}`) when their swap completes.
