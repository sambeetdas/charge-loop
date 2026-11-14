# ChargeLoop — Functional & Non-Functional Requirements Specification

**Reference Document:** ChargeLoop BRD v1.0  
**Target Release:** Phase 1 Foundation & Phase 2 Expansion  

---

## 1. Functional Requirements (FR)

### 1.1 Driver Application (DR)

| ID | Requirement Description | Priority |
| :--- | :--- | :---: |
| **DR-01** | **Authentication & Onboarding**: Sign up / Sign in using 10-digit mobile number with auto-reading SMS OTP. Language selection (Hindi, English, regional). | **Must** |
| **DR-02** | **Digital KYC & Vehicle Linking**: Capture Aadhaar (masked), Driving Licence number, and link vehicle registration number, OEM brand, and model. | **Must** |
| **DR-03** | **Live Battery State of Charge (SOC)**: Display current battery percentage, estimated driving range in km, and trigger low-charge warning (< 20%). | **Must** |
| **DR-04** | **Station Discovery & Inventory Radar**: Display list and map view of nearby stations with distance in km, operating hours, ready-to-swap battery count, and wait time. | **Must** |
| **DR-05** | **1-Tap Navigation**: Launch external turn-by-turn navigation (Google Maps / MapmyIndia) to chosen station. | **Must** |
| **DR-06** | **Battery Pre-Reservation**: Allow driver to place a 15-minute hold on a battery while en route to prevent stock-outs. | **Should** |
| **DR-07** | **2-Minute Swap Execution**: Scan station QR code or present dynamic swap token; receive real-time handshake validation in < 1.5 seconds. | **Must** |
| **DR-08** | **Subscription Management**: Purchase, renew, pause, or view validity of daily, weekly, and monthly swap subscription plans. | **Must** |
| **DR-09** | **Digital Wallet**: View real-time wallet balance, execute quick top-ups via UPI AutoPay, cards, or net banking, and pay per-swap dues. | **Must** |
| **DR-10** | **Transaction & GST Invoices**: View downloadable receipts with transparent tax breakdown (Base Fee + 18% GST). | **Must** |
| **DR-11** | **Plain-Language Cost Breakdown**: Display clear charge explanation before swap confirmation with zero hidden penalty charges. | **Must** |
| **DR-12** | **Grievance Redressal & Support**: In-app dispute raising for swap failures, payment stalls, or battery defects with live status tracking. | **Must** |
| **DR-13** | **Vernacular Voice Assistance**: Voice prompt reading station availability and battery status in Hindi and regional languages. | **Should** |
| **DR-14** | **Safety & SOS Helpline**: One-touch 24/7 SOS emergency button triggering roadside and thermal safety alerts to operations. | **Should** |
| **DR-15** | **Driver Welfare Benefits**: In-app portal to view active accidental insurance cover (₹5,00,000) and milestone loyalty rewards. | **Should** |
| **DR-16** | **Multi-Channel Notifications**: Push notifications, SMS, and WhatsApp alerts for low battery, renewal reminders, and receipts. | **Must** |
| **DR-17** | **Offline-Tolerant Caching**: Graceful operation and cached station bookmarks when transitioning through 3G/low-network zones. | **Should** |
| **DR-18** | **Credit / Debit Card Payment**: Support Credit Card and Debit Card payment flows alongside UPI, Wallet, Cash, and Subscription Passes for both wallet top-ups and direct swap payments. | **Must** |
| **DR-19** | **Refer a Friend Program**: Generate unique driver referral codes, allow referral code application during signup, and credit referral rewards to driver wallets. | **Must** |

---

### 1.2 Partner Application (PA) — Station Operators

| ID | Requirement Description | Priority |
| :--- | :--- | :---: |
| **PA-01** | **Partner Registration & Payout Setup**: Self-onboarding with business KYC, bank account / UPI ID payout configuration, and agreement e-sign. | **Must** |
| **PA-02** | **Driver Validation & Swap Execution**: Validate Driver (phone/ID, KYC, mounted battery, wallet/subscription status) before unlocking the Swap button; scan Driver's old battery QR for disconnection and scan new charged battery QR for assignment. | **Must** |
| **PA-03** | **Exception Handling**: Reject incompatible batteries, report damaged/tampered batteries, and cancel swaps with standard reason codes. | **Must** |
| **PA-04** | **Battery Registration with Store Details**: Register new battery packs (Serial, Model, SOC %, SOH %) linked directly to the partner's store/station inventory, and provide live available charged battery counts. | **Must** |
| **PA-05** | **Multi-Mode Payment Initiation & Instant Commission**: Initiate swap payment via Card, UPI, Cash, Wallet, or Weekly Subscription Pass; credit partner commission (₹25/swap) upon completion. | **Must** |
| **PA-06** | **Multi-Channel Bill Publishing & Billing History**: Publish itemized GST swap bills to the Driver via WhatsApp, SMS, and Email, and generate complete billing history in the Driver app. | **Must** |
| **PA-07** | **Daily & Monthly Earnings Dashboard**: Performance metrics, daily swap counts, revenue graphs, and incentive bonuses. | **Should** |
| **PA-08** | **Station Status Control**: Toggle station operational state (Open, Closed, Maintenance, Power Outage) reflecting instantly on driver maps. | **Must** |
| **PA-09** | **Hardware & Charger Ticket Raising**: Report station charger hardware failure or power issues directly to operations. | **Must** |
| **PA-10** | **Vernacular Training & SOP Videos**: Video guides on safety protocols, battery handling, and fire precautions in local languages. | **Should** |

---

### 1.3 Rider Application (Rental EV)

| ID | Requirement Description | Priority |
| :--- | :--- | :---: |
| **RA-01** | **Registration & Driving Licence Verification**: Rider KYC and rental plan selection. | **Must** |
| **RA-02** | **Interactive Map of Rental Hubs**: Locate available rental e-scooters and compatible swap points. | **Must** |
| **RA-03** | **Rental Booking & Smart Lock**: Digital booking, BLE/IoT unlock, and trip tracking. | **Should** |
| **RA-04** | **Rental Payments & Invoices**: Flexible hourly/daily rental deductions. | **Must** |
| **RA-05** | **Roadside Assistance**: Incident reporting and emergency support. | **Must** |

---

### 1.4 Admin & Operations Platform (AD)

| ID | Requirement Description | Priority |
| :--- | :--- | :---: |
| **AD-01** | **Role-Based Access Control (RBAC)**: Fine-grained permissions for Operations, Finance, Risk, and Support agents. | **Must** |
| **AD-02** | **Dynamic Pricing & Tax Rules**: Configure plan fees, city-level rates, GST rules, and penalties without deploying code. | **Must** |
| **AD-03** | **Partner & Station Lifecycle**: Manage station onboarding, bay hardware allocation, and site audits. | **Must** |
| **AD-04** | **Battery Registry & Telemetry Feeds**: Real-time asset registry tracking serial, SOC %, SOH %, temperature, cycle count, and GPS coordinates. | **Must** |
| **AD-05** | **Automated Safety & Theft Alerts**: Immediate alert dispatch for temperature > 55°C, cell voltage imbalance, or geofence breaches. | **Must** |
| **AD-06** | **Financial Settlement Engine**: Daily partner payouts, GST calculation, TDS compliance, and reconciliation exception handling. | **Must** |
| **AD-07** | **360° Support Console**: Unified driver and partner profile viewer with ticket handling and action macros. | **Must** |
| **AD-08** | **Fraud & Collusion Detection**: Anomaly detection for abnormal swap frequencies, clone battery serials, and station collusion. | **Should** |
| **AD-09** | **Expansion Analytics**: Demand heatmaps and predictive station placement analytics. | **Should** |
| **AD-10** | **Financial & ESG Reporting**: Automated reports on emission-free kilometers, fuel displacement, and GST ledgers. | **Must** |
| **AD-11** | **Campaign & Notification Manager**: Geo-targeted push/SMS broadcast campaigns for driver engagement. | **Should** |
| **AD-12** | **Immutable Audit Logs**: Tamper-proof logging of configuration changes, fund transfers, and user data access. | **Must** |

---

## 2. Non-Functional Requirements (NFR)

### 2.1 Performance & Latency
* **Screen Render Time**: Mobile app screens load in $\le$ 2.5 seconds on 4G networks.
* **Swap Handshake Latency**: Backend validation API response in $\le$ 1.5 seconds.
* **Geospatial Station Query**: Returns nearest stations within 10km in $\le$ 50ms.
* **Real-time Push**: SignalR broadcast of station inventory changes delivered to active mobile clients within 500ms.

### 2.2 Scalability & Throughput
* System architecture must comfortably scale to **100,000+ daily swaps**, **100+ cities**, and **10,000+ concurrently active drivers** without architectural redesign.

### 2.3 Mobile Hardware & Low-Spec Optimization
* **Target Android**: Fully compatible with Android 8.0 (API 26) through modern Android 14+.
* **Low Memory Footprint**: Maximum RAM usage under 150MB, running smoothly on budget 2GB–3GB RAM devices.
* **Sunlight Contrast**: Minimum WCAG AA 4.5:1 contrast ratio with high-visibility electric accents.
* **Touch Targets**: Minimum interactive touch target size of **52dp $\times$ 52dp** for easy tapping while wearing motorcycle gloves.

### 2.4 Security & Data Privacy
* **DPDP Act 2023 Compliance**: Explicit consent logging for all PII collection (Aadhaar, DL, GPS location). Zero cleartext storage of Aadhaar numbers (only masked tokens stored).
* **Encryption**: TLS 1.3 in transit; AES-256 for sensitive credentials and financial records at rest.
* **Financial Idempotency**: All payment and wallet debit/credit endpoints must be idempotent (`X-Idempotency-Key`) to prevent double-charging during network drops.

### 2.5 Reliability & Availability
* **Monthly Availability**: 99.9% uptime for core swap and wallet payment services.
* **Disaster Recovery**: Recovery Point Objective (RPO) $\le$ 15 minutes; Recovery Time Objective (RTO) $\le$ 1 hour.
