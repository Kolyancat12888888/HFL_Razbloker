<?php

namespace App\Services\System;

class DnsManagerService
{
    private string $sqliteDbPath = '/opt/hfl-server/data/hfl_enterprise.db';

    private function getPdo(): ?\PDO
    {
        if (file_exists($this->sqliteDbPath)) {
            return new \PDO("sqlite:{$this->sqliteDbPath}", null, null, [
                \PDO::ATTR_ERRMODE => \PDO::ERRMODE_EXCEPTION,
                \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC
            ]);
        }
        return null;
    }

    public function listRecords(): array
    {
        $pdo = $this->getPdo();
        if (!$pdo) return [];
        $stmt = $pdo->query("SELECT Id, Domain, IpAddress, IsEnabled, Note FROM DnsRecords ORDER BY Id DESC");
        return $stmt->fetchAll();
    }

    public function addOrUpdateRecord(string $domain, string $ipAddress = '31.77.8.9', string $note = ''): bool
    {
        $pdo = $this->getPdo();
        if (!$pdo) return false;

        $stmt = $pdo->prepare("
            INSERT INTO DnsRecords (Domain, IpAddress, IsEnabled, Note)
            VALUES (:domain, :ip, 1, :note)
            ON CONFLICT(Domain) DO UPDATE SET
                IpAddress = excluded.IpAddress,
                IsEnabled = excluded.IsEnabled,
                Note = excluded.Note
        ");

        return $stmt->execute([
            ':domain' => strtolower(trim($domain)),
            ':ip' => trim($ipAddress),
            ':note' => trim($note)
        ]);
    }

    public function deleteRecord(int $id): bool
    {
        $pdo = $this->getPdo();
        if (!$pdo) return false;
        $stmt = $pdo->prepare("DELETE FROM DnsRecords WHERE Id = ?");
        return $stmt->execute([$id]);
    }
}
