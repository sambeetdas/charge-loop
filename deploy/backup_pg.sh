#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="/var/backups/chargeloop"
DB_NAME="chargeloop"
DB_USER="postgres"
STAMP=$(date -u +"%Y%m%d_%H%M%S")
FILE="${BACKUP_DIR}/chargeloop_${STAMP}.sql.gz"

mkdir -p "${BACKUP_DIR}"
pg_dump -h localhost -U "${DB_USER}" -d "${DB_NAME}" | gzip > "${FILE}"

# Keep last 14 days locally
find "${BACKUP_DIR}" -type f -name "*.sql.gz" -mtime +14 -delete

# Optional: Upload to Cloudflare R2 / AWS S3 using rclone (configure `rclone config` remote named `r2`)
if command -v rclone >/dev/null 2>&1; then
  rclone copy "${FILE}" r2:chargeloop-backups/
fi

echo "Backup completed: ${FILE}"
