<?php

namespace App\Services\System;

class NginxManagerService
{
    private string $availablePath = '/etc/nginx/sites-available';
    private string $enabledPath = '/etc/nginx/sites-enabled';

    public function generateVhost(
        string $domain,
        array $aliases = [],
        string $docRoot = '',
        string $envType = 'PHP',
        string $phpVersion = '8.3',
        bool $enableSsl = true,
        int $proxyPort = 3000,
        bool $enableHsts = true,
        bool $enableGzip = true,
        bool $enableHttp2 = true
    ): string {
        $serverNames = implode(' ', array_merge([$domain], $aliases));
        $docRoot = $docRoot ?: "/var/www/{$domain}";

        $sslBlock = '';
        if ($enableSsl) {
            $sslCert = file_exists("/etc/ssl/certs/hfl-wildcard.pem") 
                ? "/etc/ssl/certs/hfl-wildcard.pem" 
                : "/etc/ssl/certs/ssl-cert-snakeoil.pem";
            $sslKey = file_exists("/etc/ssl/certs/hfl-wildcard.key") 
                ? "/etc/ssl/certs/hfl-wildcard.key" 
                : "/etc/ssl/private/ssl-cert-snakeoil.key";

            $hstsHeader = $enableHsts ? 'add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;' : '';
            $http2Flag = $enableHttp2 ? 'http2' : '';

            $sslBlock = <<<EOF

    listen 443 ssl {$http2Flag};
    listen 31.77.8.9:443 ssl {$http2Flag};
    ssl_certificate {$sslCert};
    ssl_certificate_key {$sslKey};
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    {$hstsHeader}
EOF;
        }

        $gzipBlock = $enableGzip ? <<<EOF
    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml application/json application/javascript application/rss+xml application/atom+xml image/svg+xml;
EOF : '';

        $executionBlock = '';
        if ($envType === 'PHP') {
            $executionBlock = <<<EOF
    location / {
        try_files \$uri \$uri/ /index.php?\$query_string;
    }

    location ~ \.php$ {
        include snippets/fastcgi-php.conf;
        fastcgi_pass unix:/run/php/php{$phpVersion}-fpm.sock;
        fastcgi_param SCRIPT_FILENAME \$realpath_root\$fastcgi_script_name;
        fastcgi_param DOCUMENT_ROOT \$realpath_root;
        fastcgi_read_timeout 300;
        fastcgi_buffer_size 128k;
        fastcgi_buffers 4 256k;
    }
EOF;
        } elseif ($envType === 'Node.js' || $envType === 'Proxy') {
            $executionBlock = <<<EOF
    location / {
        proxy_pass http://127.0.0.1:{$proxyPort};
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
EOF;
        } else {
            $executionBlock = <<<EOF
    location / {
        try_files \$uri \$uri/ =404;
    }
EOF;
        }

        return <<<EOF
# ==============================================================================
# Virtual Host Configuration for: {$domain}
# Managed by HFL WebPanel Enterprise
# ==============================================================================

server {
    listen 80;
    listen 31.77.8.9:80;{$sslBlock}

    server_name {$serverNames};
    root {$docRoot};
    index index.php index.html index.htm;

    charset utf-8;
    client_max_body_size 256M;

    access_log /var/log/nginx/{$domain}_access.log combined;
    error_log  /var/log/nginx/{$domain}_error.log warn;

{$gzipBlock}

{$executionBlock}

    location ~* \.(jpg|jpeg|gif|png|css|js|ico|webp|svg|woff|woff2|ttf|eot)$ {
        expires 30d;
        add_header Cache-Control "public, no-transform";
        try_files \$uri =404;
    }

    location ~ /\.(?!well-known).* {
        deny all;
    }
}
EOF;
    }

    public function applyVhost(string $domain, string $content): bool
    {
        if (!is_dir($this->availablePath)) @mkdir($this->availablePath, 0755, true);
        if (!is_dir($this->enabledPath)) @mkdir($this->enabledPath, 0755, true);

        $availableFile = "{$this->availablePath}/{$domain}.conf";
        $enabledFile = "{$this->enabledPath}/{$domain}.conf";

        file_put_contents($availableFile, $content);
        if (!file_exists($enabledFile)) {
            @symlink($availableFile, $enabledFile);
        }

        return $this->testAndReload();
    }

    public function removeVhost(string $domain): bool
    {
        $availableFile = "{$this->availablePath}/{$domain}.conf";
        $enabledFile = "{$this->enabledPath}/{$domain}.conf";

        if (file_exists($enabledFile)) @unlink($enabledFile);
        if (file_exists($availableFile)) @unlink($availableFile);

        return $this->testAndReload();
    }

    public function testAndReload(): bool
    {
        $testOutput = shell_exec("nginx -t 2>&1") ?? '';
        if (str_contains($testOutput, 'successful')) {
            shell_exec("systemctl reload nginx");
            return true;
        }
        return false;
    }
}
