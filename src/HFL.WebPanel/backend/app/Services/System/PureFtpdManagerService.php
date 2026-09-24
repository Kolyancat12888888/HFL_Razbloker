<?php

namespace App\Services\System;

class PureFtpdManagerService
{
    private string $pureDbPath = '/etc/pure-ftpd/pureftpd.pdb';

    /**
     * Create a chrooted FTP user bound strictly to their website directory
     */
    public function createFtpUser(string $username, string $password, string $homeDir = '/var/www'): bool
    {
        $username = preg_replace('/[^a-zA-Z0-9_]/', '', $username);
        if (!is_dir($homeDir)) @mkdir($homeDir, 0755, true);

        // pure-pw useradd <user> -u www-data -d <dir> -m
        $cmd = "echo " . escapeshellarg($password) . " | pure-pw useradd " . escapeshellarg($username) . " -u www-data -g www-data -d " . escapeshellarg($homeDir) . " -m 2>&1";
        shell_exec($cmd);
        shell_exec("pure-pw mkdb 2>&1");

        return true;
    }

    /**
     * Delete an FTP user
     */
    public function deleteFtpUser(string $username): bool
    {
        $username = preg_replace('/[^a-zA-Z0-9_]/', '', $username);
        shell_exec("pure-pw userdel " . escapeshellarg($username) . " -m 2>&1");
        shell_exec("pure-pw mkdb 2>&1");
        return true;
    }
}
