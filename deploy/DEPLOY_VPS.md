# ChargeLoop — AWS Lightsail Deployment Guide (Single-VPS)

**Target Specs:** AWS Lightsail **Ubuntu 24.04 LTS** instance (**Mumbai `ap-south-1`**) running **.NET 11 WebApi + Caddy (HTTPS) + PostgreSQL + Daily `pg_dump` to AWS S3**.

---

## 1. Create AWS Lightsail Instance & Open Firewall
1. Open **AWS Lightsail Console** → **Create instance**:
   - **Region**: `Mumbai (ap-south-1)`
   - **Platform**: `Linux/Unix` → **OS Only** → `Ubuntu 24.04 LTS`
   - **Plan**: `2 vCPU, 2–4 GB RAM` recommended (or `512 MB RAM` for pilot with 2 GB swap).
2. Go to your instance's **Networking** tab:
   - Click **Attach static IP** so the public IP never changes on reboot.
   - Under **IPv4 Firewall**, add rules:
     - **SSH** (`TCP 22`)
     - **HTTP** (`TCP 80`)
     - **HTTPS** (`TCP 443`)
3. *(Optional, for HTTPS)* In Cloudflare / your DNS provider, add an `A` record for your domain (e.g. `chargeloopsln.in` and `www`) pointing to your Lightsail Static IP.

---

## 2. How to Connect via SSH

### Method A: Lightsail Browser SSH (Easiest — No `.pem` Needed)
1. In **AWS Lightsail Console**, click your instance (`Ubuntu-1`).
2. Click **Connect using SSH** — a browser terminal opens directly as `ubuntu`.

### Method B: SSH from Windows PowerShell (Using `.pem` Key)
1. Download your `.pem` key from **Lightsail Console → Account (top-right) → SSH keys → Download** (e.g., `LightsailDefaultKey-ap-south-1.pem` into `Downloads`).
2. On Windows, restrict `.pem` file permissions (otherwise `ssh` will reject it):
   ```powershell
   $key = "$env:USERPROFILE\Downloads\LightsailDefaultKey-ap-south-1.pem"
   icacls $key /inheritance:r
   icacls $key /grant:r "$($env:USERNAME):(R)"
   ```
3. Connect to your server (replace `<PUBLIC_IP>` with your Lightsail IP):
   ```powershell
   ssh -i "$env:USERPROFILE\Downloads\LightsailDefaultKey-ap-south-1.pem" ubuntu@<PUBLIC_IP>
   ```

### Method C: SSH from macOS / Linux
```bash
chmod 400 ~/Downloads/LightsailDefaultKey-ap-south-1.pem
ssh -i ~/Downloads/LightsailDefaultKey-ap-south-1.pem ubuntu@<PUBLIC_IP>
```

---

## 3. One-Command Automated Setup & Deploy (Recommended)

Once connected via SSH, run:

- **HTTP on IP (e.g. `http://13.233.63.106`)**:
  ```bash
  curl -sSL https://raw.githubusercontent.com/sambeetdas/charge-loop/master/deploy/setup_lightsail.sh | sudo bash
  ```

- **With Custom Domain (Automatic HTTPS via Caddy)** and custom DB password:
  ```bash
  curl -sSL https://raw.githubusercontent.com/sambeetdas/charge-loop/master/deploy/setup_lightsail.sh | sudo DOMAIN="chargeloopsln.in" DB_PASS="YourStrongDbPass!" bash
  ```

This script automatically:
1. Creates a **2 GB swap file** (keeps 512 MB RAM instances stable).
2. Installs **PostgreSQL**, sets the `postgres` password, creates `chargeloop`, and runs `database/01_init_schema.sql`.
3. Installs **.NET 11 SDK**, clones `sambeetdas/charge-loop`, and publishes to `/opt/chargeloop/app`.
4. Configures `chargeloop.service` (`systemd`), **Caddy**, and `ufw` firewall (`22`, `80`, `443`).

---

## 4. Verify & Common Operations

### Verify App & Health
```bash
curl http://127.0.0.1:5000/health
sudo systemctl status chargeloop
sudo systemctl status caddy
```
From your browser, open `http://<PUBLIC_IP>` (or `https://chargeloopsln.in`).

### View Live Logs
```bash
sudo journalctl -u chargeloop -f
```

### Add Google Maps API Key (Optional)
Edit `/etc/systemd/system/chargeloop.service`, set `Environment="GoogleMaps__ApiKey=YOUR_KEY"`, then:
```bash
sudo systemctl daemon-reload
sudo systemctl restart chargeloop
```

### Redeploy After New Git Commits
```bash
cd /opt/chargeloop/src
sudo git pull origin master
sudo dotnet publish backend/ChargeLoop.WebApi/ChargeLoop.WebApi.csproj -c Release -o /opt/chargeloop/app
sudo chown -R www-data:www-data /opt/chargeloop/app
sudo systemctl restart chargeloop
```

---

## 5. Daily PostgreSQL Backups to AWS S3 (Mumbai)
```bash
sudo apt install -y awscli
aws configure   # Enter IAM Access Key/Secret with s3:PutObject on your bucket (ap-south-1)
sudo cp /opt/chargeloop/src/deploy/backup_pg.sh /usr/local/bin/chargeloop_backup.sh
sudo chmod +x /usr/local/bin/chargeloop_backup.sh

# Add ~/.pgpass for passwordless pg_dump in cron:
echo "localhost:5432:chargeloop:postgres:YOUR_DB_PASS" | sudo tee /root/.pgpass
sudo chmod 600 /root/.pgpass

# Schedule daily backup at 2:00 AM UTC:
(sudo crontab -l 2>/dev/null; echo "0 2 * * * S3_BUCKET=s3://chargeloop-backups-mumbai /usr/local/bin/chargeloop_backup.sh >> /var/log/chargeloop_backup.log 2>&1") | sudo crontab -
```
