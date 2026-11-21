#!/usr/bin/env bash
set -euo pipefail

# ChargeLoop One-Shot Setup & Deploy for AWS Lightsail (Ubuntu 24.04)
# Usage:
#   curl -sSL https://raw.githubusercontent.com/sambeetdas/charge-loop/master/deploy/setup_lightsail.sh | sudo bash
# Optional env vars: DB_PASS, JWT_KEY, DOMAIN (defaults to :80 HTTP on IP)

DB_PASS="${DB_PASS:-ChargeLoop_Prod_2026!}"
JWT_KEY="${JWT_KEY:-$(openssl rand -hex 32)}"
DOMAIN="${DOMAIN:-:80}"
APP_DIR="/opt/chargeloop"
REPO_DIR="/opt/chargeloop/src"

echo "=== [1/6] Configuring 2GB Swap (for 512MB RAM Lightsail) ==="
if [ ! -f /swapfile ]; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

echo "=== [2/6] Installing System Packages, PostgreSQL & Caddy ==="
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get install -y curl git ufw postgresql postgresql-contrib caddy

echo "=== [3/6] Installing .NET 11 SDK ==="
if [ ! -x /usr/local/share/dotnet/dotnet ]; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 11.0 --install-dir /usr/local/share/dotnet
  ln -sf /usr/local/share/dotnet/dotnet /usr/bin/dotnet
fi

echo "=== [4/6] Configuring PostgreSQL (chargeloop DB) ==="
systemctl enable --now postgresql
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD '${DB_PASS}';"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname = 'chargeloop'" | grep -q 1 || sudo -u postgres psql -c "CREATE DATABASE chargeloop;"

echo "=== [5/6] Cloning & Publishing ChargeLoop ==="
mkdir -p "${APP_DIR}/app"
if [ -d "${REPO_DIR}/.git" ]; then
  git -C "${REPO_DIR}" pull origin master
else
  git clone https://github.com/sambeetdas/charge-loop.git "${REPO_DIR}"
fi

PGPASSWORD="${DB_PASS}" psql -h localhost -U postgres -d chargeloop -f "${REPO_DIR}/database/01_init_schema.sql"
dotnet publish "${REPO_DIR}/backend/ChargeLoop.WebApi/ChargeLoop.WebApi.csproj" -c Release -o "${APP_DIR}/app"
chown -R www-data:www-data "${APP_DIR}/app"

echo "=== [6/6] Configuring systemd, Caddy & Firewall ==="
cat >/etc/systemd/system/chargeloop.service <<EOF
[Unit]
Description=ChargeLoop .NET 11 WebAPI & SignalR Service
After=network.target postgresql.service
Wants=postgresql.service

[Service]
WorkingDirectory=${APP_DIR}/app
ExecStart=/usr/bin/dotnet ${APP_DIR}/app/ChargeLoop.WebApi.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=chargeloop-api
User=www-data

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment="ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=chargeloop;Username=postgres;Password=${DB_PASS};"
Environment="Jwt__Key=${JWT_KEY}"
Environment="Jwt__Issuer=ChargeLoop.WebApi"
Environment="Jwt__Audience=ChargeLoop.Clients"
Environment="GoogleMaps__ApiKey="

[Install]
WantedBy=multi-user.target
EOF

cat >/etc/caddy/Caddyfile <<EOF
${DOMAIN} {
    encode gzip zstd
    reverse_proxy 127.0.0.1:5000
}
EOF

ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

systemctl daemon-reload
systemctl enable --now chargeloop
systemctl reload caddy

echo "========================================================"
echo "ChargeLoop deployed! Health check:"
curl -s http://127.0.0.1:5000/health || true
echo ""
echo "========================================================"
