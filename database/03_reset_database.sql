-- ==============================================================================
-- ChargeLoop — Teardown & Reset Database Script
-- WARNING: This will permanently delete all data in the chargeloop database.
-- ==============================================================================

DROP TABLE IF EXISTS support_tickets CASCADE;
DROP TABLE IF EXISTS reservations CASCADE;
DROP TABLE IF EXISTS driver_subscriptions CASCADE;
DROP TABLE IF EXISTS subscription_plans CASCADE;
DROP TABLE IF EXISTS wallet_transactions CASCADE;
DROP TABLE IF EXISTS swap_transactions CASCADE;
DROP TABLE IF EXISTS batteries CASCADE;
DROP TABLE IF EXISTS vehicles CASCADE;
DROP TABLE IF EXISTS drivers CASCADE;
DROP TABLE IF EXISTS stations CASCADE;
DROP TABLE IF EXISTS partners CASCADE;
