# 🗄️ ChargeLoop PostgreSQL Database Setup Guide

This directory contains the complete database scripts, migrations, seed records, and automated deployment scripts for the **ChargeLoop EV Battery Swapping Network**.

---

## 📁 Directory Contents

| File | Purpose |
| :--- | :--- |
| `01_init_schema.sql` | **Core DDL**: Creates all 11 tables, constraints, foreign keys, and performance indexes. |
| `02_seed_data.sql` | **Initial Seed**: Populates realistic NCR stations, partners, drivers, vehicles, batteries, and subscription passes. |
| `03_reset_database.sql` | **Teardown**: Drops all tables cleanly with cascade rules for a fresh reset. |
| `setup_database.ps1` | **Automated Windows/PowerShell script** to check connection, create DB, and apply scripts. |
| `setup_database.sh` | **Automated Linux/macOS bash script** for CI/CD and container workflows. |

---

## 🚀 Quick Setup (Choose One Option)

### Option 1: Automated PowerShell Script (Recommended for Windows)

Run the automated script from PowerShell:

```powershell
# From the project root:
.\database\setup_database.ps1
```

#### Custom credentials or clean reset:
```powershell
.\database\setup_database.ps1 -PgUser postgres -PgPassword "YourPassword" -DbName chargeloop -Reset
```

The script automatically detects `psql.exe` (including default installation directories like `C:\Program Files\PostgreSQL\18\bin\psql.exe`), creates the database if it doesn't already exist, applies schema and seed data, and outputs an inventory summary.

---

### Option 2: Manual Setup via `psql` CLI

If you prefer running commands manually via standard PostgreSQL terminal tools:

#### Step 1: Open Terminal & Connect to PostgreSQL
```bash
psql -h localhost -p 5432 -U postgres
```

#### Step 2: Create the Database
```sql
CREATE DATABASE chargeloop;
\q
```

#### Step 3: Run the Schema & Seed Scripts
```bash
# Apply schema
psql -h localhost -p 5432 -U postgres -d chargeloop -f database/01_init_schema.sql

# Populate initial seed data
psql -h localhost -p 5432 -U postgres -d chargeloop -f database/02_seed_data.sql
```

---

### Option 3: Docker Compose Setup

If you prefer running PostgreSQL inside a Docker container:

```bash
# Launch PostgreSQL 16 with PostGIS in background
docker compose up -d postgres
```

The `docker-compose.yml` mounts this directory to automatically execute on container initialization.

---

### Option 4: Graphical Setup via pgAdmin 4 or DBeaver

1. Open **pgAdmin 4** (or **DBeaver**).
2. Connect to your PostgreSQL server (`localhost:5432`).
3. Right-click on **Databases** &rarr; **Create** &rarr; **Database...**
   - Database Name: `chargeloop`
   - Owner: `postgres`
   - Click **Save**.
4. Right-click on `chargeloop` &rarr; Open **Query Tool**.
5. Open and execute `database/01_init_schema.sql` (Press `F5`).
6. Open and execute `database/02_seed_data.sql` (Press `F5`).

---

## 🔑 Connection Strings

### ASP.NET Core (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chargeloop;Username=postgres;Password=Sambeet@123"
  }
}
```

### Python (asyncpg / SQLAlchemy):
```python
DATABASE_URL = "postgresql+asyncpg://postgres:Sambeet%40123@localhost:5432/chargeloop"
```

---

## 📊 Database Schema Dictionary

The system manages 11 core relational tables:

```mermaid
erDiagram
    PARTNERS ||--o{ STATIONS : operates
    PARTNERS ||--o{ SWAP_TRANSACTIONS : earns_commission
    STATIONS ||--o{ BATTERIES : houses_inventory
    STATIONS ||--o{ SWAP_TRANSACTIONS : hosts_swap
    STATIONS ||--o{ RESERVATIONS : holds_battery
    DRIVERS ||--o{ VEHICLES : owns
    DRIVERS ||--o{ BATTERIES : uses_active
    DRIVERS ||--o{ SWAP_TRANSACTIONS : initiates
    DRIVERS ||--o{ DRIVER_SUBSCRIPTIONS : subscribes
    SUBSCRIPTION_PLANS ||--o{ DRIVER_SUBSCRIPTIONS : defines
    DRIVERS ||--o{ RESERVATIONS : books
    DRIVERS ||--o{ SUPPORT_TICKETS : files_grievance
```

### Table Summary:

1. **`partners`**: Station vendor partners and franchisee operators managing swapping hubs.
2. **`stations`**: Physical battery swapping hubs, locations (lat/long), bay capacity, and live status.
3. **`drivers`**: EV drivers, KYC status (Aadhaar/DL), language code, and live wallet balance.
4. **`vehicles`**: Linked electric 2-wheelers and 3-wheelers with OEM model and battery compatibility type.
5. **`batteries`**: Smart battery packs, tracking State of Charge (SOC %), State of Health (SOH %), temperature, cycles, and current station/driver assignment.
6. **`swap_transactions`**: Historical audit log of every 2-minute battery exchange, duration seconds, fee breakdown (Base + GST), and vendor commission.
7. **`wallet_transactions`**: Immutable financial ledger tracking credits, debits, UPI top-ups, and payouts.
8. **`subscription_plans`**: Available gig delivery passes (Daily, Weekly Express, Monthly Pro).
9. **`driver_subscriptions`**: Driver's active subscription period and remaining swap allocations.
10. **`reservations`**: 15-minute battery hold requests placed by en-route drivers.
11. **`support_tickets`**: Operational grievances and station hardware defect tickets.

---

## 🔍 Verification Queries

Run these queries in `psql` or pgAdmin to verify your setup:

```sql
-- 1. Check all stations with live ready battery counts
SELECT s.name, s.city, s.total_bays, s.available_charged_batteries, p.business_name AS vendor
FROM stations s
JOIN partners p ON s.partner_id = p.id;

-- 2. Check drivers with linked vehicles and wallet balances
SELECT d.full_name, d.phone_number, d.wallet_balance, v.registration_number, v.oem
FROM drivers d
LEFT JOIN vehicles v ON d.id = v.driver_id;

-- 3. Check inventory status of all batteries
SELECT 
    CASE status
        WHEN 0 THEN 'Available Charged'
        WHEN 1 THEN 'Charging in Station'
        WHEN 2 THEN 'In Use by Driver'
        ELSE 'Maintenance'
    END AS status_name,
    COUNT(*) AS total_count
FROM batteries
GROUP BY status;
```

---

## 🛠️ Resetting the Database

To wipe existing tables and reset to a clean state:

```powershell
.\database\setup_database.ps1 -Reset
```
Or via SQL:
```bash
psql -h localhost -p 5432 -U postgres -d chargeloop -f database/03_reset_database.sql
```
