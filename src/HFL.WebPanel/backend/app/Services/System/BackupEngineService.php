<?php

namespace App\Services\System;

class BackupEngineService
{
    private string $backupStorageDir = '/var/backups/hfl-panel';

    public function createFullBackup(): array
    {
        if (!is_dir($this->backupStorageDir)) @mkdir($this->backupStorageDir, 0755, true);

        $date = date('Y-m-d_His');
        $filename = "hfl_full_backup_{$date}.tar.gz";
        $targetFile = "{$this->backupStorageDir}/{$filename}";

        // 1. Dump all MySQL databases to temp
        $sqlDumpPath = sys_get_temp_dir() . "/all_dbs_{$date}.sql";
        $dbPass = env('DB_PASSWORD', '');
        $passArg = $dbPass ? "-p" . escapeshellarg($dbPass) : "";
        shell_exec("mysqldump -u " . escapeshellarg(env('DB_USERNAME', 'root')) . " {$passArg} --all-databases > {$sqlDumpPath} 2>/dev/null");

        // 2. Compress /var/www and sql dump
        $cmd = "tar -czf " . escapeshellarg($targetFile) . " -C /var/www . -C " . escapeshellarg(sys_get_temp_dir()) . " " . escapeshellarg(basename($sqlDumpPath)) . " 2>&1";
        shell_exec($cmd);

        @unlink($sqlDumpPath);

        return [
            'filename' => $filename,
            'size' => file_exists($targetFile) ? round(filesize($targetFile) / 1024 / 1024, 2) . ' MB' : '0 MB',
            'path' => $targetFile,
            'createdAt' => date('Y-m-d H:i:s')
        ];
    }
}
