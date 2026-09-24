#!/bin/bash
set -e

echo "🚀 Установка HFL Razbloker Enterprise Server на Linux..."

# Check root
if [ "$EUID" -ne 0 ]; then
  echo "❌ Пожалуйста, запустите скрипт от root (sudo ./install.sh)"
  exit 1
fi

# 1. Free Port 53 from systemd-resolved and pdns / named / dnsmasq if active
echo "🔓 Освобождение UDP порта 53 для HFL DNS сервера..."
systemctl stop pdns pdns-recursor bind9 named dnsmasq systemd-resolved 2>/dev/null || true
systemctl disable pdns pdns-recursor bind9 named dnsmasq systemd-resolved 2>/dev/null || true

if systemctl is-active --quiet systemd-resolved 2>/dev/null; then
    mkdir -p /etc/systemd/resolved.conf.d/
    cat << 'EOF' > /etc/systemd/resolved.conf.d/disable-stub.conf
[Resolve]
DNSStubListener=no
EOF
    systemctl restart systemd-resolved 2>/dev/null || true
fi

# Set local DNS resolver
if [ -f /etc/resolv.conf ]; then
    chattr -i /etc/resolv.conf 2>/dev/null || true
    echo -e "nameserver 127.0.0.1\nnameserver 1.1.1.1" > /etc/resolv.conf 2>/dev/null || true
fi

# 2. Install .NET 9 if not present
if ! command -v dotnet &> /dev/null; then
    echo "📦 Установка .NET 9 SDK..."
    if [ -f /etc/debian_version ]; then
        apt-get update
        apt-get install -y wget curl
        wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb 2>/dev/null || wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        apt-get update
        apt-get install -y dotnet-sdk-9.0
    fi
fi

# 3. Build Server
echo "🔨 Сборка HFL Server..."
dotnet publish src/HFL.Server/HFL.Server.csproj -c Release -r linux-x64 --self-contained false -o /opt/hfl-server

mkdir -p /opt/hfl-server/data

# 4. Deploy Nginx diagnostics site for *.local / *.internal
echo "🌐 Настройка Nginx для *.local..."
mkdir -p /var/www/hfl-local
cp deploy/nginx/index.html /var/www/hfl-local/index.html 2>/dev/null || true

# Generate snakeoil SSL cert if missing
if [ ! -f /etc/ssl/certs/ssl-cert-snakeoil.pem ]; then
    apt-get install -y ssl-cert 2>/dev/null || openssl req -x509 -nodes -days 3650 -newkey rsa:2048 -keyout /etc/ssl/private/ssl-cert-snakeoil.key -out /etc/ssl/certs/ssl-cert-snakeoil.pem -subj "/CN=*.local" 2>/dev/null || true
fi

if [ -d /etc/nginx/sites-available ]; then
    cp deploy/nginx/hfl-local.conf /etc/nginx/sites-available/hfl-local.conf
    ln -sf /etc/nginx/sites-available/hfl-local.conf /etc/nginx/sites-enabled/hfl-local.conf
    nginx -t && systemctl reload nginx 2>/dev/null || true
fi

# 5. Create systemd service
cat << 'EOF' > /etc/systemd/system/hfl-server.service
[Unit]
Description=HFL Razbloker Enterprise Server (DoH + DNS + Telegram Bot + License API)
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/opt/hfl-server
ExecStart=/usr/bin/dotnet /opt/hfl-server/HFL.Server.dll
Restart=always
RestartSec=3
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=BOT_TOKEN=8649333793:AAFTEfrJiqN0FQyLPuL3Sx0idg0earZHoA8
Environment=ADMIN_TELEGRAM_ID=6014501462

[Install]
WantedBy=multi-user.target
EOF

# 5. Start service
systemctl daemon-reload
systemctl enable hfl-server
systemctl restart hfl-server

sleep 2

echo "✅ HFL Server успешно запущен на порту 53 (DNS) и 5000 (API/DoH)!"
echo "📊 Статус службы: systemctl status hfl-server"
echo "📜 Логи: journalctl -u hfl-server -f"
