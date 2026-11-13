#!/usr/bin/env bash
# ==============================================================================
# ChargeLoop — Linux/macOS Automated PostgreSQL Setup Script
# ==============================================================================

set -e

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
PGPASSWORD="${PGPASSWORD:-Sambeet@123}"
DBNAME="${DBNAME:-chargeloop}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export PGPASSWORD

echo "⚡ ChargeLoop PostgreSQL Setup & Migration"
echo "Target: $PGHOST:$PGPORT | Database: $DBNAME | User: $PGUSER"

# Check if database exists
EXISTS=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT 1 FROM pg_database WHERE datname='$DBNAME';" 2>/dev/null || true)

if [ "$EXISTS" != "1" ]; then
    echo "Creating database '$DBNAME'..."
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -c "CREATE DATABASE $DBNAME;"
fi

echo "Applying schema (01_init_schema.sql)..."
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$DBNAME" -f "$SCRIPT_DIR/01_init_schema.sql"

echo "Applying seed data (02_seed_data.sql)..."
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$DBNAME" -f "$SCRIPT_DIR/02_seed_data.sql"

echo "✓ PostgreSQL setup completed successfully!"
