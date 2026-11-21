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

# Upload to AWS S3 (set S3_BUCKET env var, e.g. s3://chargeloop-backups-mumbai) or Cloudflare R2 via rclone
if [ -n "${S3_BUCKET:-}" ] && command -v aws >/dev/null 2>&1; then
  aws s3 cp "${FILE}" "${S3_BUCKET}/" --only-show-errors
elif command -v rclone >/dev/null 2>&1; then
  rclone copy "${FILE}" r2:chargeloop-backups/
fi

echo "Backup completed: ${FILE}"
