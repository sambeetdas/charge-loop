# ChargeLoop — Single-VPS Deployment Guide (Option B)

**Target Specs:** 1 Ubuntu 24.04 VPS (2 vCPU, 4 GB RAM, 40 GB SSD) running **.NET 11 WebApi + Caddy (HTTPS) + PostgreSQL 18 + Daily `pg_dump` Backups**.

---

## 1. Point Your Domain DNS
In Cloudflare (or your DNS provider), create an `A` record pointing your domain (e.g. `chargeloopsln.in` and `www`) to your VPS public IPv4 address.

## 2. Firewall Setup
```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## 3. Install .NET 11 Runtime, PostgreSQL 18 & Caddy
```bash
sudo apt update && sudo apt upgrade -y
# Install Caddy
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.tar.gz' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update && sudo apt install -y caddy

# Install PostgreSQL 18 & .NET 11 ASP.NET Core Runtime (via official Microsoft packages / dotnet-install)
```
Create the database and set a strong `postgres` password:
```bash
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'CHANGE_ME_STRONG_PASSWORD';"
sudo -u postgres psql -c "CREATE DATABASE chargeloop;"
sudo -u postgres psql -d chargeloop -f database/01_init_schema.sql
```

## 4. Publish & Deploy ChargeLoop WebApi
On your build machine (or on the VPS with .NET 11 SDK):
```bash
dotnet publish backend/ChargeLoop.WebApi/ChargeLoop.WebApi.csproj -c Release -o /opt/chargeloop/app
sudo chown -R www-data:www-data /opt/chargeloop/app
```

## 5. Configure `systemd` & `Caddyfile`
1. Edit `deploy/chargeloop.service` with your real DB password, 32+ char `Jwt__Key`, and `GoogleMaps__ApiKey`.
2. Install and start the service:
```bash
sudo cp deploy/chargeloop.service /etc/systemd/system/chargeloop.service
sudo systemctl daemon-reload
sudo systemctl enable --now chargeloop
sudo systemctl status chargeloop
```
3. Copy `deploy/Caddyfile` (update domain name) and reload Caddy:
```bash
sudo cp deploy/Caddyfile /etc/caddy/Caddyfile
sudo systemctl reload caddy
```
Verify: `https://chargeloopsln.in/health` should return `Healthy`.

## 6. Daily `pg_dump` Backup to Cloudflare R2 / S3
```bash
sudo cp deploy/backup_pg.sh /usr/local/bin/chargeloop_backup.sh
sudo chmod +x /usr/local/bin/chargeloop_backup.sh
# Configure ~/.pgpass for passwordless cron pg_dump and `rclone config` for R2/S3
# Add daily 2:00 AM UTC cron:
(crontab -l 2>/dev/null; echo "0 2 * * * /usr/local/bin/chargeloop_backup.sh >> /var/log/chargeloop_backup.log 2>&1") | crontab -
```
