#!/bin/bash
set -e

echo "🚀 Установка HFL Razbloker Enterprise Server на Linux..."

# Check root
if [ "$EUID" -ne 0 ]; then
  echo "❌ Пожалуйста, запустите скрипт от root (sudo ./install.sh)"
  exit 1
fi

# 1. Install .NET 9 if not present
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

# 2. Build Server
echo "🔨 Сборка HFL Server..."
dotnet publish src/HFL.Server/HFL.Server.csproj -c Release -r linux-x64 --self-contained false -o /opt/hfl-server

mkdir -p /opt/hfl-server/data

# 3. Create systemd service
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
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=BOT_TOKEN=8649333793:AAFTEfrJiqN0FQyLPuL3Sx0idg0earZHoA8
Environment=ADMIN_TELEGRAM_ID=6014501462

[Install]
WantedBy=multi-user.target
EOF

# 4. Start service
systemctl daemon-reload
systemctl enable hfl-server
systemctl restart hfl-server

echo "✅ HFL Server успешно установлен и запущен как системная служба!"
echo "📊 Статус службы: systemctl status hfl-server"
echo "📜 Логи: journalctl -u hfl-server -f"
