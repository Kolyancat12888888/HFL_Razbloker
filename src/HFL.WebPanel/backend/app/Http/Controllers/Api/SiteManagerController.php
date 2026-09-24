<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class SiteManagerController extends Controller
{
    private string $nginxSitesAvailable = '/etc/nginx/sites-available';
    private string $nginxSitesEnabled = '/etc/nginx/sites-enabled';
    private string $wwwRoot = '/var/www';

    /**
     * List all real virtual hosts parsed directly from Nginx configurations
     */
    public function index(): JsonResponse
    {
        $sites = [];
        $id = 1;

        if (is_dir($this->nginxSitesAvailable)) {
            $files = glob("{$this->nginxSitesAvailable}/*.conf");
            foreach ($files as $file) {
                $content = file_get_contents($file);
                $filename = basename($file);

                // Extract server_name
                preg_match('/server_name\s+([^;]+);/', $content, $mServerName);
                // Extract root
                preg_match('/root\s+([^;]+);/', $content, $mRoot);
                // Extract PHP version / proxy
                $envType = 'Static';
                $phpVersion = '';
                if (preg_match('/fastcgi_pass\s+unix:\/run\/php\/php([\d\.]+)-fpm\.sock;/', $content, $mPhp)) {
                    $envType = 'PHP';
                    $phpVersion = $mPhp[1];
                } elseif (preg_match('/proxy_pass\s+http:\/\/(127\.0\.0\.1|localhost):(\d+);/', $content, $mProxy)) {
                    $envType = 'Node.js/Proxy';
                    $phpVersion = "Port {$mProxy[2]}";
                }

                // Check SSL
                $sslActive = (strpos($content, 'ssl_certificate') !== false);
                $sslType = $sslActive ? 'X.509 SSL' : 'None';
                if ($sslActive && (strpos($content, 'snakeoil') !== false || strpos($content, 'hfl') !== false)) {
                    $sslType = 'HFL SAN SSL';
                } elseif ($sslActive && strpos($content, 'letsencrypt') !== false) {
                    $sslType = "Let's Encrypt";
                }

                $domainList = !empty($mServerName[1]) ? preg_split('/\s+/', trim($mServerName[1])) : [$filename];
                $primaryDomain = $domainList[0] ?? $filename;
                $aliases = array_slice($domainList, 1);

                $isEnabled = file_exists("{$this->nginxSitesEnabled}/{$filename}");

                $sites[] = [
                    'id' => $id++,
                    'configFile' => $filename,
                    'domain' => $primaryDomain,
                    'aliases' => $aliases,
                    'docRoot' => $mRoot[1] ?? '/var/www/' . $primaryDomain,
                    'phpVersion' => $phpVersion ?: '8.3',
                    'sslActive' => $sslActive,
                    'sslType' => $sslType,
                    'status' => $isEnabled ? 'active' : 'suspended',
                    'envType' => $envType,
                ];
            }
        }

        return response()->json($sites);
    }

    /**
     * Create a new real website, Nginx vhost, directory and SSL config
     */
    public function store(Request $request): JsonResponse
    {
        $domain = strtolower(trim($request->input('domain')));
        $aliases = $request->input('aliases', []);
        $phpVersion = $request->input('phpVersion', '8.3');
        $envType = $request->input('envType', 'PHP');
        $enableSsl = (bool)$request->input('enableSsl', true);

        if (empty($domain)) {
            return response()->json(['error' => 'Domain name is required'], 422);
        }

        $docRoot = "{$this->wwwRoot}/{$domain}";
        if (!is_dir($docRoot)) {
            @mkdir($docRoot, 0755, true);
            file_put_contents("{$docRoot}/index.php", "<?php echo '<h1>Site {$domain} is working!</h1>'; phpinfo();");
            @chown($docRoot, 'www-data');
            @chgrp($docRoot, 'www-data');
        }

        $allDomains = array_merge([$domain], $aliases);
        $serverNameStr = implode(' ', $allDomains);

        $sslBlock = "";
        if ($enableSsl) {
            $sslCert = "/etc/ssl/certs/ssl-cert-snakeoil.pem";
            $sslKey = "/etc/ssl/private/ssl-cert-snakeoil.key";
            if (!file_exists($sslCert)) {
                $sslCert = "/etc/ssl/certs/hfl-wildcard.pem";
                $sslKey = "/etc/ssl/certs/hfl-wildcard.key";
            }
            $sslBlock = "
    listen 443 ssl;
    listen 31.77.8.9:443 ssl;
    ssl_certificate {$sslCert};
    ssl_certificate_key {$sslKey};
    ssl_protocols TLSv1.2 TLSv1.3;";
        }

        $phpBlock = "";
        if ($envType === 'PHP') {
            $phpBlock = "
    location ~ \.php$ {
        include snippets/fastcgi-php.conf;
        fastcgi_pass unix:/run/php/php{$phpVersion}-fpm.sock;
    }";
        }

        $vhostConfig = <<<EOF
server {
    listen 80;
    listen 31.77.8.9:80;{$sslBlock}

    server_name {$serverNameStr};
    root {$docRoot};
    index index.php index.html index.htm;

    location / {
        try_files \$uri \$uri/ /index.php?\$query_string;
    }
{$phpBlock}

    location ~ /\.ht {
        deny all;
    }
}
EOF;

        $confPath = "{$this->nginxSitesAvailable}/{$domain}.conf";
        file_put_contents($confPath, $vhostConfig);
        @symlink($confPath, "{$this->nginxSitesEnabled}/{$domain}.conf");

        // Test and reload Nginx
        $test = shell_exec("nginx -t 2>&1");
        if (strpos($test, 'successful') !== false) {
            shell_exec("systemctl reload nginx");
        }

        // Register in HFL DNS database if domain is .local or .internal
        if (str_ends_with($domain, '.local') || str_ends_with($domain, '.internal')) {
            $this->registerDnsRecord($domain, '31.77.8.9');
        }

        return response()->json(['success' => true, 'domain' => $domain, 'docRoot' => $docRoot]);
    }

    /**
     * Delete website, disable Nginx vhost and optionally delete files
     */
    public function destroy(string $domain, Request $request): JsonResponse
    {
        $confEnabled = "{$this->nginxSitesEnabled}/{$domain}.conf";
        $confAvailable = "{$this->nginxSitesAvailable}/{$domain}.conf";

        if (file_exists($confEnabled)) @unlink($confEnabled);
        if (file_exists($confAvailable)) @unlink($confAvailable);

        if ($request->input('deleteFiles', false)) {
            $docRoot = "{$this->wwwRoot}/{$domain}";
            if (is_dir($docRoot) && !in_array($docRoot, ['/var/www', '/var/www/html'])) {
                shell_exec("rm -rf " . escapeshellarg($docRoot));
            }
        }

        shell_exec("nginx -t 2>&1 && systemctl reload nginx");

        return response()->json(['success' => true]);
    }

    private function registerDnsRecord(string $domain, string $ip): void
    {
        $dbPath = '/opt/hfl-server/data/hfl_enterprise.db';
        if (file_exists($dbPath)) {
            try {
                $sqlite = new \PDO("sqlite:" . $dbPath);
                $stmt = $sqlite->prepare("INSERT OR REPLACE INTO DnsRecords (Domain, IpAddress, IsEnabled, Note) VALUES (?, ?, 1, 'Auto-generated by HFL WebPanel')");
                $stmt->execute([$domain, $ip]);
            } catch (\Exception $e) {}
        }
    }
}
