<?php

namespace App\Services\System;

class PhpFpmManagerService
{
    private string $poolDir = '/etc/php';

    /**
     * Get list of installed PHP versions on the server (e.g. 7.4, 8.0, 8.1, 8.2, 8.3, 8.4)
     */
    public function getInstalledVersions(): array
    {
        $versions = [];
        $candidates = ['7.4', '8.0', '8.1', '8.2', '8.3', '8.4'];

        foreach ($candidates as $ver) {
            if (file_exists("/usr/sbin/php-fpm{$ver}") || file_exists("/etc/php/{$ver}/fpm")) {
                $versions[] = $ver;
            }
        }

        return !empty($versions) ? $versions : ['8.3'];
    }

    /**
     * Generate an isolated PHP-FPM pool configuration for a specific user and website
     */
    public function createPool(
        string $domain,
        string $user = 'www-data',
        string $group = 'www-data',
        string $phpVersion = '8.3',
        int $maxChildren = 10,
        int $startServers = 2,
        int $minSpare = 1,
        int $maxSpare = 3
    ): string {
        $poolName = preg_replace('/[^a-zA-Z0-9_]/', '_', $domain);
        $sockPath = "/run/php/php{$phpVersion}-fpm-{$poolName}.sock";

        $config = <<<EOF
; ==============================================================================
; PHP-FPM Pool Configuration for: {$domain}
; Managed by HFL WebPanel Enterprise
; ==============================================================================

[{$poolName}]
user = {$user}
group = {$group}
listen = {$sockPath}
listen.owner = www-data
listen.group = www-data
listen.mode = 0660

pm = dynamic
pm.max_children = {$maxChildren}
pm.start_servers = {$startServers}
pm.min_spare_servers = {$minSpare}
pm.max_spare_servers = {$maxSpare}
pm.max_requests = 500

php_admin_value[open_basedir] = /var/www/{$domain}:/tmp:/var/tmp
php_admin_value[upload_max_filesize] = 128M
php_admin_value[post_max_size] = 128M
php_admin_value[memory_limit] = 256M
php_admin_value[max_execution_time] = 180
EOF;

        $targetDir = "{$this->poolDir}/{$phpVersion}/fpm/pool.d";
        if (!is_dir($targetDir)) @mkdir($targetDir, 0755, true);

        $targetFile = "{$targetDir}/{$poolName}.conf";
        file_put_contents($targetFile, $config);

        shell_exec("systemctl reload php{$phpVersion}-fpm 2>/dev/null || true");

        return $sockPath;
    }
}
