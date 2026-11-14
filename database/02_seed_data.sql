-- ==============================================================================
-- ChargeLoop EV Battery Swapping Network — Initial Seed Data
-- ==============================================================================

-- 1. Seed Partners (Station Vendors & Hub Operators)
INSERT INTO partners (id, full_name, business_name, phone_number, upi_id, wallet_balance)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Ramesh Kumar', 'Ramesh Kirana & EV Swap Point', '9876543210', 'ramesh@okhdfcbank', 2450.00),
    ('22222222-2222-2222-2222-222222222222', 'Imran Khan', 'Khan Auto Care & Swapping', '9812345678', 'imran@paytm', 5120.00)
ON CONFLICT (id) DO NOTHING;

-- 2. Seed Swapping Stations (Delhi NCR Hubs)
INSERT INTO stations (id, partner_id, name, address, city, latitude, longitude, total_bays, status, available_charged_batteries, charging_batteries, estimated_wait_minutes)
VALUES 
    ('aaaa1111-aaaa-1111-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Janakpuri Hub', 'District Centre, Janakpuri, West Delhi', 'New Delhi', 28.6289, 77.0818, 16, 0, 8, 6, 2),
    ('bbbb2222-bbbb-2222-bbbb-222222222222', '22222222-2222-2222-2222-222222222222', 'ChargeLoop Connaught Place Central', 'Outer Circle, Block L, Connaught Place', 'New Delhi', 28.6327, 77.2195, 20, 0, 12, 5, 0),
    ('cccc3333-cccc-3333-cccc-333333333333', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Noida Sector 62 Park', 'Near Metro Station, Sector 62', 'Noida', 28.6258, 77.3649, 12, 0, 5, 6, 4),
    ('dddd4444-dddd-4444-dddd-444444444444', '22222222-2222-2222-2222-222222222222', 'ChargeLoop Cyber City DLF', 'Cyber Hub, DLF Phase 2', 'Gurugram', 28.4950, 77.0890, 18, 0, 10, 4, 1),
    ('eeee5555-eeee-5555-eeee-555555555555', '11111111-1111-1111-1111-111111111111', 'ChargeLoop Rohini Sector 10', 'Swarn Jayanti Park Road, Sector 10', 'New Delhi', 28.7150, 77.1120, 14, 0, 7, 5, 3)
ON CONFLICT (id) DO NOTHING;

-- 3. Seed Registered Drivers
INSERT INTO drivers (id, phone_number, full_name, email, language_code, kyc_status, driving_license_number, aadhaar_masked, wallet_balance, referral_code)
VALUES 
    ('a0000001-0000-0000-0000-000000000001', '+919811223344', 'Amit Verma', 'amit.verma@chargeloop.in', 'en', 1, 'DL-042021004567', 'XXXXXXXX4567', 450.00, 'CL-AMIT01'),
    ('a0000002-0000-0000-0000-000000000002', '+919822334455', 'Rajesh Yadav', 'rajesh.yadav@chargeloop.in', 'en', 1, 'DL-052022007890', 'XXXXXXXX7890', 620.00, 'CL-RAJE02'),
    ('a0000003-0000-0000-0000-000000000003', '+919833445566', 'Sunita Devi', 'sunita.devi@chargeloop.in', 'en', 1, 'DL-062023001122', 'XXXXXXXX1122', 890.00, 'CL-SUNI03'),
    ('a0000004-0000-0000-0000-000000000004', '+919844556677', 'Mohammad Arif', 'arif.mohammad@chargeloop.in', 'en', 1, 'DL-082021009988', 'XXXXXXXX9988', 310.00, 'CL-ARIF04')
ON CONFLICT (id) DO NOTHING;

-- 4. Seed Linked Electric Vehicles
INSERT INTO vehicles (id, driver_id, registration_number, oem, model, battery_compatibility_type)
VALUES 
    ('e0000001-0000-0000-0000-000000000001', 'a0000001-0000-0000-0000-000000000001', 'DL 01 AB 1234', 'Quantum Energy', 'Plaza EV 2W', '2W_48V'),
    ('e0000002-0000-0000-0000-000000000002', 'a0000002-0000-0000-0000-000000000002', 'DL 04 CD 5678', 'Hero Electric', 'Optima CX 2.0', '2W_48V'),
    ('e0000003-0000-0000-0000-000000000003', 'a0000003-0000-0000-0000-000000000003', 'DL 07 EF 9012', 'Kinetic Green', 'Zing HSS', '2W_48V'),
    ('e0000004-0000-0000-0000-000000000004', 'a0000004-0000-0000-0000-000000000004', 'DL 02 GH 3456', 'Okinawa', 'Praise Pro', '2W_48V')
ON CONFLICT (id) DO NOTHING;

-- 5. Seed Smart Battery Packs (Ready at stations and active mounted on vehicles)
INSERT INTO batteries (id, serial_number, model_type, state_of_charge_percent, state_of_health_percent, cell_temperature_celsius, cycle_count, status, current_station_id, current_driver_id)
VALUES 
    -- Batteries ready at Janakpuri Station
    ('b0000001-0000-0000-0000-000000000001', 'BAT-48V-1001', '2W_48V', 100, 99, 31.0, 45, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    ('b0000002-0000-0000-0000-000000000002', 'BAT-48V-1002', '2W_48V', 98, 97, 30.5, 62, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    ('b0000003-0000-0000-0000-000000000003', 'BAT-48V-1003', '2W_48V', 95, 96, 31.2, 70, 0, 'aaaa1111-aaaa-1111-aaaa-111111111111', NULL),
    -- Batteries ready at Connaught Place Station
    ('b0000004-0000-0000-0000-000000000004', 'BAT-48V-2001', '2W_48V', 100, 100, 29.5, 20, 0, 'bbbb2222-bbbb-2222-bbbb-222222222222', NULL),
    ('b0000005-0000-0000-0000-000000000005', 'BAT-48V-2002', '2W_48V', 99, 98, 30.0, 35, 0, 'bbbb2222-bbbb-2222-bbbb-222222222222', NULL),
    -- Mounted on Drivers' Vehicles (depleted/active in use)
    ('b0000006-0000-0000-0000-000000000006', 'BAT-48V-9999', '2W_48V', 16, 95, 38.2, 180, 2, NULL, 'a0000001-0000-0000-0000-000000000001'),
    ('b0000007-0000-0000-0000-000000000007', 'BAT-48V-3001', '2W_48V', 42, 94, 34.0, 125, 2, NULL, 'a0000002-0000-0000-0000-000000000002'),
    ('b0000008-0000-0000-0000-000000000008', 'BAT-48V-3002', '2W_48V', 28, 93, 35.5, 140, 2, NULL, 'a0000003-0000-0000-0000-000000000003'),
    ('b0000009-0000-0000-0000-000000000009', 'BAT-48V-3003', '2W_48V', 12, 92, 39.1, 195, 2, NULL, 'a0000004-0000-0000-0000-000000000004')
ON CONFLICT (serial_number) DO UPDATE SET 
    current_station_id = EXCLUDED.current_station_id,
    current_driver_id = EXCLUDED.current_driver_id,
    status = EXCLUDED.status,
    state_of_charge_percent = EXCLUDED.state_of_charge_percent;

-- 6. Seed Subscription Plans
INSERT INTO subscription_plans (id, name, description, validity_days, max_swaps_allowed, price, is_active)
VALUES 
    ('c0000001-0000-0000-0000-000000000001', 'Daily Gig Unlimited', 'Unlimited swaps for delivery riders with 24h validity', 1, 5, 199.00, true),
    ('c0000002-0000-0000-0000-000000000002', 'Weekly Delivery Express', '25 swaps across all Delhi NCR hubs for 7 days', 7, 25, 899.00, true),
    ('c0000003-0000-0000-0000-000000000003', 'Monthly Commercial Pro', '100 swaps with priority queue pass and 30-day validity', 30, 100, 2999.00, true)
ON CONFLICT (id) DO NOTHING;
