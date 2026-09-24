<?php

namespace App\Services\System;

class SslCertificateManagerService
{
    private string $certsDir = '/opt/hfl-server/data/certs';
    private string $systemSslDir = '/etc/ssl/certs';

    public function getOrGenerateSanCertificate(string $domain, array $altNames = []): array
    {
        if (!is_dir($this->certsDir)) @mkdir($this->certsDir, 0755, true);

        $sanList = array_unique(array_merge([$domain], $altNames, ['*.local', '*.internal', 'test.local', 'panel.local']));
        $sanString = implode(',', array_map(fn($d) => "DNS:{$d}", $sanList)) . ",IP:31.77.8.9,IP:127.0.0.1";

        $certPath = "{$this->certsDir}/{$domain}.crt";
        $keyPath = "{$this->certsDir}/{$domain}.key";

        if (file_exists($certPath) && file_exists($keyPath)) {
            return [
                'certPath' => $certPath,
                'keyPath' => $keyPath,
                'certPem' => file_get_contents($certPath)
            ];
        }

        // OpenSSL configuration for SAN X.509 v3 extension
        $configContent = <<<EOF
[req]
distinguished_name = req_distinguished_name
x509_extensions = v3_req
prompt = no

[req_distinguished_name]
C = RU
ST = Moscow
L = Moscow
O = HFL Razbloker Enterprise
OU = Cloud Security
CN = {$domain}

[v3_req]
keyUsage = digitalSignature, keyEncipherment, keyCertSign, cRLSign
extendedKeyUsage = serverAuth, clientAuth
basicConstraints = CA:TRUE
subjectAltName = {$sanString}
EOF;

        $tmpConfig = tempnam(sys_get_temp_dir(), 'openssl_san_');
        file_put_contents($tmpConfig, $configContent);

        $cmd = "openssl req -x509 -nodes -days 3650 -newkey rsa:2048 -keyout {$keyPath} -out {$certPath} -config {$tmpConfig} 2>&1";
        shell_exec($cmd);
        @unlink($tmpConfig);

        return [
            'certPath' => $certPath,
            'keyPath' => $keyPath,
            'certPem' => file_exists($certPath) ? file_get_contents($certPath) : ''
        ];
    }
}
