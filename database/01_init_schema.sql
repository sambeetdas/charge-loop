-- ==============================================================================
-- ChargeLoop EV Battery Swapping Network — PostgreSQL Schema Definition
-- Database: chargeloop
-- Compatible with: PostgreSQL 14, 15, 16, 17, 18+
-- ==============================================================================

-- Enable UUID extension if available
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. PARTNERS (Station Vendors & Hub Operators)
CREATE TABLE IF NOT EXISTS partners (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(150) NOT NULL,
    business_name VARCHAR(200) NOT NULL,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    upi_id VARCHAR(100),
    bank_account_number VARCHAR(50),
    wallet_balance NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. STATIONS (Swapping Hubs operated by Partners)
CREATE TABLE IF NOT EXISTS stations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    partner_id UUID NOT NULL REFERENCES partners(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    address TEXT NOT NULL,
    city VARCHAR(100) NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    total_bays INT NOT NULL DEFAULT 12,
    status INT NOT NULL DEFAULT 0, -- 0=Open, 1=Closed, 2=Maintenance
    open_time TIME NOT NULL DEFAULT '06:00:00',
    close_time TIME NOT NULL DEFAULT '23:00:00',
    available_charged_batteries INT NOT NULL DEFAULT 0,
    charging_batteries INT NOT NULL DEFAULT 0,
    estimated_wait_minutes INT NOT NULL DEFAULT 0,
    last_status_update TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. DRIVERS (EV Commercial & Individual Drivers)
CREATE TABLE IF NOT EXISTS drivers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150),
    language_code VARCHAR(10) NOT NULL DEFAULT 'en',
    kyc_status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Verified, 2=Rejected
    driving_license_number VARCHAR(50),
    aadhaar_masked VARCHAR(20),
    wallet_balance NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    active_subscription_id UUID,
    referral_code VARCHAR(30) UNIQUE,
    referred_by_code VARCHAR(30),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 4. VEHICLES (Linked 2W/3W Electric Vehicles)
CREATE TABLE IF NOT EXISTS vehicles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    registration_number VARCHAR(50) NOT NULL UNIQUE,
    oem VARCHAR(100) NOT NULL,
    model VARCHAR(100) NOT NULL,
    battery_compatibility_type VARCHAR(50) NOT NULL DEFAULT '2W_48V',
    linked_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 5. BATTERIES (Tracked Smart Battery Packs)
CREATE TABLE IF NOT EXISTS batteries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    model_type VARCHAR(50) NOT NULL DEFAULT '2W_48V',
    state_of_charge_percent INT NOT NULL DEFAULT 100,
    state_of_health_percent INT NOT NULL DEFAULT 100,
    cell_temperature_celsius DOUBLE PRECISION NOT NULL DEFAULT 30.0,
    cycle_count INT NOT NULL DEFAULT 0,
    status INT NOT NULL DEFAULT 0, -- 0=AvailableCharged, 1=ChargingInStation, 2=InUseByDriver, 3=MaintenanceFaulty
    current_station_id UUID REFERENCES stations(id) ON DELETE SET NULL,
    current_driver_id UUID REFERENCES drivers(id) ON DELETE SET NULL,
    last_telemetry_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 6. SWAP_TRANSACTIONS (2-Minute Battery Swapping Records)
CREATE TABLE IF NOT EXISTS swap_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_id UUID NOT NULL REFERENCES drivers(id),
    station_id UUID NOT NULL REFERENCES stations(id),
    partner_id UUID NOT NULL REFERENCES partners(id),
    depleted_battery_id UUID NOT NULL REFERENCES batteries(id),
    depleted_battery_soc INT NOT NULL,
    issued_battery_id UUID NOT NULL REFERENCES batteries(id),
    issued_battery_soc INT NOT NULL DEFAULT 100,
    initiated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    duration_seconds INT,
    status INT NOT NULL DEFAULT 0, -- 0=Initiated, 1=Validated, 2=InProgress, 3=Completed, 4=Failed
    base_fee NUMERIC(10, 2) NOT NULL DEFAULT 100.00,
    tax_amount NUMERIC(10, 2) NOT NULL DEFAULT 18.00,
    total_amount NUMERIC(10, 2) NOT NULL DEFAULT 118.00,
    partner_commission_earned NUMERIC(10, 2) NOT NULL DEFAULT 25.00,
    payment_method INT NOT NULL DEFAULT 1, -- 0=SubscriptionQuota, 1=Wallet, 2=UPI, 3=Cash, 4=Card
    invoice_number VARCHAR(50),
    bill_sent_channels VARCHAR(100),
    notes TEXT
);

-- 7. WALLET_TRANSACTIONS (Double-Entry Financial Ledger)
CREATE TABLE IF NOT EXISTS wallet_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_type INT NOT NULL, -- 0=Driver, 1=Partner
    account_id UUID NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    type INT NOT NULL, -- 0=Credit, 1=Debit
    reason VARCHAR(255) NOT NULL,
    reference_id VARCHAR(100),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 8. SUBSCRIPTION_PLANS (Swap Passes & Gig Plans)
CREATE TABLE IF NOT EXISTS subscription_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL,
    description TEXT NOT NULL,
    validity_days INT NOT NULL,
    max_swaps_allowed INT NOT NULL,
    price NUMERIC(10, 2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 9. DRIVER_SUBSCRIPTIONS (Active Passes assigned to Drivers)
CREATE TABLE IF NOT EXISTS driver_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES subscription_plans(id) ON DELETE CASCADE,
    start_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    end_date TIMESTAMPTZ NOT NULL,
    swaps_used INT NOT NULL DEFAULT 0,
    swaps_total_allowed INT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 10. RESERVATIONS (15-Minute Battery Hold at Stations)
CREATE TABLE IF NOT EXISTS reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    station_id UUID NOT NULL REFERENCES stations(id) ON DELETE CASCADE,
    reserved_battery_id UUID REFERENCES batteries(id) ON DELETE SET NULL,
    reserved_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    status INT NOT NULL DEFAULT 0 -- 0=Active, 1=Fulfilled, 2=Expired, 3=Cancelled
);

-- 11. SUPPORT_TICKETS (Driver Grievances & Hardware Malfunctions)
CREATE TABLE IF NOT EXISTS support_tickets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_type INT NOT NULL, -- 0=Driver, 1=Partner
    user_id UUID NOT NULL,
    category VARCHAR(100) NOT NULL,
    subject VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    status INT NOT NULL DEFAULT 0, -- 0=Open, 1=InProgress, 2=Resolved
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 12. OTP_VERIFICATIONS (Phone OTP Registration & Sign-In)
CREATE TABLE IF NOT EXISTS otp_verifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone_number VARCHAR(20) NOT NULL,
    otp_code VARCHAR(10) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ==============================================================================
-- Performance Indexes
-- ==============================================================================
CREATE INDEX IF NOT EXISTS idx_stations_partner_id ON stations(partner_id);
CREATE INDEX IF NOT EXISTS idx_vehicles_driver_id ON vehicles(driver_id);
CREATE INDEX IF NOT EXISTS idx_batteries_station_id ON batteries(current_station_id);
CREATE INDEX IF NOT EXISTS idx_batteries_driver_id ON batteries(current_driver_id);
CREATE INDEX IF NOT EXISTS idx_swap_driver_id ON swap_transactions(driver_id);
CREATE INDEX IF NOT EXISTS idx_swap_station_id ON swap_transactions(station_id);
CREATE INDEX IF NOT EXISTS idx_wallet_account ON wallet_transactions(account_id, account_type);
CREATE INDEX IF NOT EXISTS idx_reservations_driver_station ON reservations(driver_id, station_id);
CREATE INDEX IF NOT EXISTS idx_otp_phone ON otp_verifications(phone_number);

-- Unified Financial Transactions Table (Idempotent by correlation_id)
CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY,
    correlation_id UUID NOT NULL UNIQUE,
    user_id UUID NOT NULL,
    user_role VARCHAR(20) NOT NULL,
    station_id UUID,
    swap_id UUID,
    transaction_type VARCHAR(40) NOT NULL,
    payment_mode VARCHAR(20) NOT NULL DEFAULT 'CASH',
    amount NUMERIC(12,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Completed',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_tx_user ON transactions(user_id, created_at DESC);

-- End-to-End Audit Logs Table (Tracked by correlation_id)
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY,
    correlation_id UUID NOT NULL,
    event_name VARCHAR(80) NOT NULL,
    actor_id UUID,
    actor_role VARCHAR(20),
    http_method VARCHAR(10),
    endpoint VARCHAR(255),
    status_code INT,
    duration_ms BIGINT,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_audit_corr ON audit_logs(correlation_id, created_at DESC);
