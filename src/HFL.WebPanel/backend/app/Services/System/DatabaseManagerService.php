<?php

namespace App\Services\System;

class DatabaseManagerService
{
    private ?\PDO $pdo = null;

    public function __construct()
    {
        $this->connect();
    }

    private function connect(): void
    {
        try {
            $host = env('DB_HOST', '127.0.0.1');
            $port = env('DB_PORT', '3306');
            $user = env('DB_USERNAME', 'root');
            $pass = env('DB_PASSWORD', '');
            $this->pdo = new \PDO("mysql:host={$host};port={$port}", $user, $pass, [
                \PDO::ATTR_ERRMODE => \PDO::ERRMODE_EXCEPTION,
                \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC
            ]);
        } catch (\Exception $e) {
            $this->pdo = null;
        }
    }

    public function listDatabases(): array
    {
        if (!$this->pdo) return [];
        $stmt = $this->pdo->query("
            SELECT 
                table_schema AS name,
                ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS size_mb,
                COUNT(table_name) AS tables_count
            FROM information_schema.tables 
            WHERE table_schema NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys')
            GROUP BY table_schema
        ");
        return $stmt->fetchAll();
    }

    public function createDatabase(string $name, string $charset = 'utf8mb4', string $collation = 'utf8mb4_unicode_ci'): bool
    {
        if (!$this->pdo) return false;
        $name = preg_replace('/[^a-zA-Z0-9_]/', '', $name);
        $this->pdo->exec("CREATE DATABASE IF NOT EXISTS `{$name}` CHARACTER SET {$charset} COLLATE {$collation}");
        return true;
    }

    public function createUser(string $username, string $password, string $host = 'localhost'): bool
    {
        if (!$this->pdo) return false;
        $username = preg_replace('/[^a-zA-Z0-9_]/', '', $username);
        $this->pdo->exec("CREATE USER IF NOT EXISTS '{$username}'@'{$host}' IDENTIFIED BY " . $this->pdo->quote($password));
        return true;
    }

    public function grantPrivileges(string $database, string $username, string $host = 'localhost'): bool
    {
        if (!$this->pdo) return false;
        $database = preg_replace('/[^a-zA-Z0-9_]/', '', $database);
        $username = preg_replace('/[^a-zA-Z0-9_]/', '', $username);
        $this->pdo->exec("GRANT ALL PRIVILEGES ON `{$database}`.* TO '{$username}'@'{$host}'");
        $this->pdo->exec("FLUSH PRIVILEGES");
        return true;
    }

    public function exportDump(string $database): string
    {
        $database = preg_replace('/[^a-zA-Z0-9_]/', '', $database);
        $user = env('DB_USERNAME', 'root');
        $pass = env('DB_PASSWORD', '');
        $passArg = $pass ? "-p" . escapeshellarg($pass) : "";
        $cmd = "mysqldump -u " . escapeshellarg($user) . " {$passArg} " . escapeshellarg($database);
        return shell_exec($cmd) ?? '';
    }
}
