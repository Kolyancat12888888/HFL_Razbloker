# 🛡️ HFL Razbloker Enterprise

Полностью автономный **Enterprise Unblocker & VPN комплекс** на базе **C# / .NET 9** с трехуровневой системой обхода блокировок («Швейцарские часы»), встроенным DNS/DoH сервером, локальными приватными доменами (`.local`, `.internal`), аппаратным лицензированием по HWID и Telegram-ботом.

---

## ⚡ Особенности и возможности

1. **Автономное ядро «Швейцарские часы» (Пинг < 80 мс):**
   - **Уровень 1 (Zapret WinDivert / winws):** Мгновенный локальный обход DPI без прокси (YouTube 4K, Discord Voice, блокировки сайтов) с нулевой задержкой.
   - **Уровень 2 (Zapret Disorder/BadSeq + Custom DoH):** Автоматическое переключение при фильтрации провайдером.
   - **Уровень 3 (3X-UI VLESS Reality + Direct RU Split):** Автономный туннель с интеллектуальным сплит-роутингом (трафик РФ идет напрямую с пингом 5–20 мс, а заблокированный — через оптимизированный VLESS Reality сервер).
2. **Собственный DNS / DoH Сервер:**
   - Поддержка стандартного DNS (UDP 53) и RFC 8484 DNS-over-HTTPS (`/dns-query`).
   - Upstream перенаправление на Cloudflare (`1.1.1.1`) для всех обычных сайтов интернета.
   - **Скрытые локальные домены:** Автоматический резолвинг `.local`, `.internal` и любых кастомных доменов на IP вашего сервера. Все подключенные клиенты бесшовно получают к ним доступ!
3. **Аппаратное лицензирование (HWID Protection):**
   - Уникальный отпечаток ПК (CPU, Motherboard, BIOS, MachineGuid с HMAC SHA256).
   - Защита от передачи ключа третьим лицам.
4. **Управление через Telegram-бота (C# Telegram.Bot):**
   - Удобные Inline-кнопки без лишних команд.
   - Генерация ключей: 7, 30, 90, 365 дней или Бессрочно.
   - Управление: просмотр привязки HWID, сброс HWID, продление, блокировка.
   - Управление локальными DNS-записями прямо из чата.
5. **Enterprise CI/CD (GitHub Actions):**
   - Автоматическая сборка самодостаточного Single-File `HFL_Razbloker.exe`.
   - Специальное версионирование с переносом на 30 (`1.0.0` $\rightarrow$ `1.0.30` $\rightarrow$ `1.1.1`).

---

## 📂 Структура проекта

```
HFL_Razbloker/
├── src/
│   ├── HFL.Core/              # Ядро: модели, HWID генератор, DNS протокол
│   ├── HFL.Server/            # ASP.NET Core 9: DoH сервер, DNS 53, Telegram-бот, API
│   └── HFL.Client/            # WPF клиент: Modern Glass Dark UI, Swiss Clock Engine
├── .github/workflows/         # CI/CD: Release сборка и авто-версионирование
├── Dockerfile                 # Multi-stage сборка сервера
├── docker-compose.yml         # Развертывание сервера в 1 команду
└── README.md
```

---

## 🚀 Быстрый старт: Развертывание сервера на Linux VPS (Без Docker)

Сервер разворачивается на Ubuntu/Debian одной командой:

```bash
git clone https://github.com/Kolyancat12888888/HFL_Razbloker.git
cd HFL_Razbloker
chmod +x install.sh
sudo ./install.sh
```

Скрипт автоматически:
1. Установит .NET 9 на сервер.
2. Соберет и развернет сервер в `/opt/hfl-server`.
3. Создаст и запустит фоновую системную службу `systemd` (`hfl-server.service`).
4. Автоматически запустит Telegram-бота с твоим токеном и DoH/DNS сервер на порту 5000 и 53!

### Полезные команды на сервере:
* **Статус службы:** `systemctl status hfl-server`
* **Логи в реальном времени:** `journalctl -u hfl-server -f`
* **Перезапуск:** `systemctl restart hfl-server`

---

## 🌐 Инструкция: Как сделать сайты на `.local` / `.internal` доступными пользователям

Когда пользователи подключаются через **HFL Razbloker**, их запросы идут через ваш DoH/DNS сервер. Все домены `.local` и `.internal` резолвятся на IP вашего сервера, где их встречает **Nginx**.

### 1. Настройка Nginx для приватных доменов

Создайте конфигурационный файл на VPS (например, `/etc/nginx/sites-available/internal_sites.conf`):

```nginx
# Виртуальный хост для secret.local
server {
    listen 80;
    listen 443 ssl http2;
    server_name secret.local *.secret.local;

    ssl_certificate     /etc/ssl/internal/secret.local.crt;
    ssl_certificate_key /etc/ssl/internal/secret.local.key;

    # Каталог с файлами сайта
    root /var/www/secret_local;
    index index.html index.htm;

    location / {
        try_files $uri $uri/ /index.html;
    }
}

# Виртуальный хост для panel.internal (прокси на внутренний сервис, например порт 3000)
server {
    listen 80;
    listen 443 ssl http2;
    server_name panel.internal;

    ssl_certificate     /etc/ssl/internal/panel.internal.crt;
    ssl_certificate_key /etc/ssl/internal/panel.internal.key;

    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Активируйте сайт и перезапустите Nginx:
```bash
ln -s /etc/nginx/sites-available/internal_sites.conf /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

---

## 🔒 Пошаговое руководство: SSL/TLS сертификаты для `.local` и `.internal` (Зеленый замочек)

Так как публичные центры сертификации (Let's Encrypt) не выдают сертификаты для доменов верхнего уровня вроде `.local` и `.internal`, идеальный и самый простой способ — использовать **mkcert** или собственный локальный **Root CA**:

### Шаг 1. Установка `mkcert` на сервере или ПК
```bash
# На Ubuntu/Debian:
apt install libnss3-tools
curl -JLO "https://dl.filippo.io/mkcert/latest?for=linux/amd64"
chmod +x mkcert-v*-linux-amd64
mv mkcert-v*-linux-amd64 /usr/local/bin/mkcert
```

### Шаг 2. Создание доверенного Root CA и сертификата для доменов
```bash
# Инициализация корневого сертификата
mkcert -install

# Создание сертификата для всех локальных доменов
mkdir -p /etc/ssl/internal
cd /etc/ssl/internal
mkcert -cert-file secret.local.crt -key-file secret.local.key "secret.local" "*.secret.local" "panel.internal" "*.internal" "*.local"
```

### Шаг 3. Доверие сертификату на клиентских ПК
`mkcert` создаст файл корневого сертификата `rootCA.pem` (путь можно узнать командой `mkcert -CAROOT`).
* Достаточно один раз импортировать этот `rootCA.pem` в «Доверенные корневые центры сертификации» Windows (или распространить вместе с клиентом).
* **Результат:** Браузеры (Chrome, Firefox, Edge, Яндекс) открывают `https://secret.local` с **зеленым замочком** и полноценным TLS-шифрованием без каких-либо предупреждений!

---

## 🤖 Управление через Telegram-бота

В боте доступно кнопочное Inline-меню:

1. 🔑 **Создать ключ:**
   - Выбор срока: `7 дней`, `30 дней`, `90 дней`, `365 дней`, `Бессрочный` или `Свой срок`.
   - Бот генерирует ключ формата `HFL-XXXX-XXXX-XXXX`, готовый к копированию в один клик.
2. 📋 **Список ключей:**
   - Интерактивная пагинация, просмотр статуса (🟢 Активен, 🟡 Не активирован, 🔴 Заблокирован).
   - Кнопка **Сбросить HWID** при смене ПК пользователем.
   - Кнопка **Блокировки / Разблокировки**.
3. 🌐 **DNS & Локальные домены:**
   - Просмотр и добавление записей прямо из чата (например: `*.local 127.0.0.1` или `mysite.internal 194.87.x.x`).
4. 📊 **Enterprise Статистика:**
   - Общее число лицензий, активных пользователей и статус серверов.

---

## 🛠️ Сборка клиента и CI/CD

Для локальной сборки клиента:
```bash
dotnet publish src/HFL.Client/HFL.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Исполняемый файл `HFL_Razbloker.exe` готов к работе без необходимости установки .NET на конечном компьютере.
