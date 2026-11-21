-- ChargeLoop Core Schema for PostgreSQL

CREATE TABLE IF NOT EXISTS partners (
    id UUID PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    business_name VARCHAR(200) NOT NULL,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    email VARCHAR(150),
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
    is_visible_to_driver BOOLEAN NOT NULL DEFAULT TRUE,
    last_telemetry_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS swap_transactions (
    id UUID PRIMARY KEY,
    driver_id UUID NOT NULL REFERENCES drivers(id),
    station_id UUID NOT NULL REFERENCES stations(id),
    partner_id UUID NOT NULL REFERENCES partners(id),
    depleted_battery_id UUID NOT NULL REFERENCES batteries(id),
    depleted_battery_soc INT NOT NULL,
    depleted_battery_id UUID REFERENCES batteries(id),
    depleted_battery_soc INT NOT NULL DEFAULT 0,
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
    swap_otp VARCHAR(10),
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

CREATE TABLE IF NOT EXISTS subscription_plans (
    id UUID PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    description TEXT NOT NULL,
    validity_days INT NOT NULL,
    max_swaps_allowed INT NOT NULL,
    price NUMERIC(10, 2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS driver_subscriptions (
    id UUID PRIMARY KEY,
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES subscription_plans(id) ON DELETE CASCADE,
    start_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    end_date TIMESTAMPTZ NOT NULL,
    swaps_used INT NOT NULL DEFAULT 0,
    swaps_total_allowed INT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS reservations (
    id UUID PRIMARY KEY,
    driver_id UUID NOT NULL REFERENCES drivers(id) ON DELETE CASCADE,
    station_id UUID NOT NULL REFERENCES stations(id) ON DELETE CASCADE,
    reserved_battery_id UUID REFERENCES batteries(id) ON DELETE SET NULL,
    otp_code VARCHAR(10) NOT NULL DEFAULT '',
    reserved_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    status INT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS support_tickets (
    id UUID PRIMARY KEY,
    user_type INT NOT NULL,
    user_id UUID NOT NULL,
    category VARCHAR(100) NOT NULL,
    subject VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    status INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS pricing_configs (
    id UUID PRIMARY KEY,
    base_swap_fee NUMERIC(10, 2) NOT NULL DEFAULT 100.00,
    platform_fee NUMERIC(10, 2) NOT NULL DEFAULT 10.00,
    convenience_fee NUMERIC(10, 2) NOT NULL DEFAULT 5.00,
    payment_gateway_fee NUMERIC(10, 2) NOT NULL DEFAULT 2.50,
    bank_charges NUMERIC(10, 2) NOT NULL DEFAULT 1.50,
    gst_percent NUMERIC(5, 2) NOT NULL DEFAULT 18.00,
    partner_commission NUMERIC(10, 2) NOT NULL DEFAULT 25.00,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS battery_types (
    id UUID PRIMARY KEY,
    code VARCHAR(60) NOT NULL UNIQUE,
    display_name VARCHAR(150) NOT NULL,
    voltage INT NOT NULL DEFAULT 48,
    capacity_ah INT NOT NULL DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Seed Initial Partners
INSERT INTO partners (id, full_name, business_name, phone_number, upi_id, wallet_balance)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Ramesh Kumar', 'Ramesh Kirana & EV Swap Point', '9876543210', 'ramesh@okhdfcbank', 2450.00),
    ('22222222-2222-2222-2222-222222222222', 'Imran Khan', 'Khan Auto Care & Swapping', '9812345678', 'imran@paytm', 5120.00)
ON CONFLICT (id) DO NOTHING;

-- Seed Stations in Delhi NCR
INSERT INTO stations (id, partner_id, name, address, city, latitude, longitude, total_bays, status, available_charged_batteries, charging_batteries, estimated_wait_minutes)
VALUES 
    ('aaaa1111-aaaa-1111-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Janakpuri Hub', 'District Centre, Janakpuri, West Delhi', 'New Delhi', 28.6289, 77.0818, 16, 0, 8, 6, 2),
    ('bbbb2222-bbbb-2222-bbbb-222222222222', '22222222-2222-2222-2222-222222222222', 'ChargeLoop Connaught Place Central', 'Outer Circle, Block L, Connaught Place', 'New Delhi', 28.6327, 77.2195, 20, 0, 12, 5, 0),
    ('cccc3333-cccc-3333-cccc-333333333333', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Noida Sector 62 Park', 'Near Metro Station, Sector 62', 'Noida', 28.6258, 77.3649, 12, 0, 5, 6, 4),
    ('dddd4444-dddd-4444-dddd-444444444444', '22222222-2222-2222-2222-222222222222', 'ChargeLoop Cyber City DLF', 'Cyber Hub, DLF Phase 2', 'Gurugram', 28.4950, 77.0890, 18, 0, 10, 4, 1),
    ('eeee5555-eeee-5555-eeee-555555555555', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Rohini Sector 10', 'Swarn Jayanti Park Road, Sector 10', 'New Delhi', 28.7150, 77.1120, 14, 0, 7, 5, 3)
ON CONFLICT (id) DO NOTHING;

-- Seed Diverse Drivers
INSERT INTO drivers (id, phone_number, full_name, language_code, kyc_status, driving_license_number, aadhaar_masked, wallet_balance)
VALUES 
    ('a0000001-0000-0000-0000-000000000001', '+919811223344', 'Amit Verma', 'hi', 1, 'DL-042021004567', 'XXXXXXXX4567', 450.00),
    ('a0000002-0000-0000-0000-000000000002', '+919822334455', 'Rajesh Yadav', 'hi', 1, 'DL-052022007890', 'XXXXXXXX7890', 620.00),
    ('a0000003-0000-0000-0000-000000000003', '+919833445566', 'Sunita Devi', 'en', 1, 'DL-062023001122', 'XXXXXXXX1122', 890.00),
    ('a0000004-0000-0000-0000-000000000004', '+919844556677', 'Mohammad Arif', 'hi', 1, 'DL-082021009988', 'XXXXXXXX9988', 310.00)
ON CONFLICT (id) DO NOTHING;

-- Seed Vehicles for Drivers
INSERT INTO vehicles (id, driver_id, registration_number, oem, model, battery_compatibility_type)
VALUES 
    ('e0000001-0000-0000-0000-000000000001', 'a0000001-0000-0000-0000-000000000001', 'DL 01 AB 1234', 'Quantum Energy', 'Plaza EV 2W', '2W_48V'),
    ('e0000002-0000-0000-0000-000000000002', 'a0000002-0000-0000-0000-000000000002', 'DL 04 CD 5678', 'Hero Electric', 'Optima CX 2.0', '2W_48V'),
    ('e0000003-0000-0000-0000-000000000003', 'a0000003-0000-0000-0000-000000000003', 'DL 07 EF 9012', 'Kinetic Green', 'Zing HSS', '2W_48V'),
    ('e0000004-0000-0000-0000-000000000004', 'a0000004-0000-0000-0000-000000000004', 'DL 02 GH 3456', 'Okinawa', 'Praise Pro', '2W_48V')
ON CONFLICT (id) DO NOTHING;

-- Seed Batteries (Ready at stations and Active in Drivers' EVs)
INSERT INTO batteries (id, serial_number, model_type, state_of_charge_percent, state_of_health_percent, cell_temperature_celsius, cycle_count, status, current_station_id, current_driver_id)
VALUES 
    -- Batteries at Janakpuri Station
    ('b0000001-0000-0000-0000-000000000001', 'BAT-48V-1001', '2W_48V', 100, 99, 31.0, 45, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    ('b0000002-0000-0000-0000-000000000002', 'BAT-48V-1002', '2W_48V', 98, 97, 30.5, 62, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    ('b0000003-0000-0000-0000-000000000003', 'BAT-48V-1003', '2W_48V', 95, 96, 31.2, 70, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    -- Batteries at Connaught Place Station
    ('b0000004-0000-0000-0000-000000000004', 'BAT-48V-2001', '2W_48V', 100, 100, 29.5, 20, 0, 'bbbb2222-bbbb-2222-bbbb-222222222222', NULL),
    ('b0000005-0000-0000-0000-000000000005', 'BAT-48V-2002', '2W_48V', 99, 98, 30.0, 35, 0, 'bbbb2222-bbbb-2222-bbbb-222222222222', NULL),
    -- Batteries Mounted on Drivers' Vehicles (depleted / active in use)
    ('b0000006-0000-0000-0000-000000000006', 'BAT-48V-9999', '2W_48V', 16, 95, 38.2, 180, 2, NULL, 'a0000001-0000-0000-0000-000000000001'),
    ('b0000007-0000-0000-0000-000000000007', 'BAT-48V-3001', '2W_48V', 42, 94, 34.0, 125, 2, NULL, 'a0000002-0000-0000-0000-000000000002'),
    ('b0000008-0000-0000-0000-000000000008', 'BAT-48V-3002', '2W_48V', 28, 93, 35.5, 140, 2, NULL, 'a0000003-0000-0000-0000-000000000003'),
    ('b0000009-0000-0000-0000-000000000009', 'BAT-48V-3003', '2W_48V', 12, 92, 39.1, 195, 2, NULL, 'a0000004-0000-0000-0000-000000000004')
ON CONFLICT (serial_number) DO UPDATE SET 
    current_station_id = EXCLUDED.current_station_id,
    current_driver_id = EXCLUDED.current_driver_id,
    status = EXCLUDED.status,
    state_of_charge_percent = EXCLUDED.state_of_charge_percent;

-- Seed Subscription Plans
INSERT INTO subscription_plans (id, name, description, validity_days, max_swaps_allowed, price, is_active)
VALUES 
    ('c0000001-0000-0000-0000-000000000001', 'Daily Gig Unlimited', 'Unlimited swaps for delivery drivers with 24h validity', 1, 5, 199.00, true),
    ('c0000002-0000-0000-0000-000000000002', 'Weekly Delivery Express', '25 swaps across all Delhi NCR hubs for 7 days', 7, 25, 899.00, true),
    ('c0000003-0000-0000-0000-000000000003', 'Monthly Commercial Pro', '100 swaps with priority queue pass and 30-day validity', 30, 100, 2999.00, true)
ON CONFLICT (id) DO NOTHING;
