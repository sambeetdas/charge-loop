-- ChargeLoop Core Schema for PostgreSQL

CREATE TABLE IF NOT EXISTS partners (
    id UUID PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    business_name VARCHAR(200) NOT NULL,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    upi_id VARCHAR(100),
    bank_account_number VARCHAR(50),
    wallet_balance NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS stations (
    id UUID PRIMARY KEY,
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

CREATE TABLE IF NOT EXISTS drivers (
    id UUID PRIMARY KEY,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    full_name VARCHAR(150) NOT NULL,
    language_code VARCHAR(10) NOT NULL DEFAULT 'hi',
    kyc_status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Verified, 2=Rejected
    driving_license_number VARCHAR(50),
    aadhaar_masked VARCHAR(20),
    wallet_balance NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    active_subscription_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vehicles (
    id UUID PRIMARY KEY,
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    registration_number VARCHAR(50) NOT NULL UNIQUE,
    oem VARCHAR(100) NOT NULL,
    model VARCHAR(100) NOT NULL,
    battery_compatibility_type VARCHAR(50) NOT NULL DEFAULT '2W_48V',
    linked_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS batteries (
    id UUID PRIMARY KEY,
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

CREATE TABLE IF NOT EXISTS swap_transactions (
    id UUID PRIMARY KEY,
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
    payment_method INT NOT NULL DEFAULT 1,
    notes TEXT
);

CREATE TABLE IF NOT EXISTS wallet_transactions (
    id UUID PRIMARY KEY,
    account_type INT NOT NULL, -- 0=Driver, 1=Partner
    account_id UUID NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    type INT NOT NULL, -- 0=Credit, 1=Debit
    reason VARCHAR(255) NOT NULL,
    reference_id VARCHAR(100),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed Initial Partners
INSERT INTO partners (id, full_name, business_name, phone_number, upi_id, wallet_balance)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Ramesh Kumar', 'Ramesh Kirana & EV Swap Point', '9876543210', 'ramesh@okhdfcbank', 2450.00),
    ('22222222-2222-2222-2222-222222222222', 'Imran Khan', 'Khan Auto Care & Swapping', '9812345678', 'imran@paytm', 5120.00)
ON CONFLICT (id) DO NOTHING;

-- Seed Initial Stations in Delhi NCR
INSERT INTO stations (id, partner_id, name, address, city, latitude, longitude, total_bays, status, available_charged_batteries, charging_batteries, estimated_wait_minutes)
VALUES 
    ('aaaa1111-aaaa-1111-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Janakpuri Hub', 'District Centre, Janakpuri, West Delhi', 'New Delhi', 28.6289, 77.0818, 16, 0, 8, 6, 2),
    ('bbbb2222-bbbb-2222-bbbb-222222222222', '22222222-2222-2222-2222-222222222222', 'ChargeLoop Connaught Place Central', 'Outer Circle, Block L, Connaught Place', 'New Delhi', 28.6327, 77.2195, 20, 0, 12, 5, 0),
    ('cccc3333-cccc-3333-cccc-333333333333', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Noida Sector 62 Park', 'Near Metro Station, Sector 62', 'Noida', 28.6258, 77.3649, 12, 0, 5, 6, 4)
ON CONFLICT (id) DO NOTHING;

-- Seed Initial Driver
INSERT INTO drivers (id, phone_number, full_name, language_code, kyc_status, driving_license_number, aadhaar_masked, wallet_balance)
VALUES 
    ('99999999-9999-9999-9999-999999999999', '+919876500001', 'Ravi Sharma', 'hi', 1, 'DL-0420200012345', 'XXXXXXXX1234', 450.00)
ON CONFLICT (id) DO NOTHING;

-- Seed Initial Vehicle
INSERT INTO vehicles (id, driver_id, registration_number, oem, model, battery_compatibility_type)
VALUES 
    ('88888888-8888-8888-8888-888888888888', '99999999-9999-9999-9999-999999999999', 'DL 01 AB 1234', 'Quantum Energy', 'Plaza EV 2W', '2W_48V')
ON CONFLICT (id) DO NOTHING;

-- Seed Initial Batteries
INSERT INTO batteries (id, serial_number, model_type, state_of_charge_percent, state_of_health_percent, cell_temperature_celsius, cycle_count, status, current_station_id)
VALUES 
    ('10000001-0000-0000-0000-000000000001', 'BAT-48V-1001', '2W_48V', 100, 99, 31.0, 45, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111'),
    ('10000002-0000-0000-0000-000000000002', 'BAT-48V-1002', '2W_48V', 100, 97, 30.5, 62, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111'),
    ('10000003-0000-0000-0000-000000000003', 'BAT-48V-9999', '2W_48V', 16, 95, 38.2, 180, 2, NULL)
ON CONFLICT (id) DO NOTHING;
