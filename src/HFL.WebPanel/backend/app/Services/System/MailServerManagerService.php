<?php

namespace App\Services\System;

class MailServerManagerService
{
    private string $vmailDir = '/var/vmail';
    private string $postfixDir = '/etc/postfix';
    private string $dkimDir = '/etc/opendkim/keys';

    /**
     * Create a new mail domain with DKIM key generation
     */
    public function createMailDomain(string $domain): array
    {
        $domain = strtolower(trim($domain));
        $domainDir = "{$this->vmailDir}/{$domain}";
        if (!is_dir($domainDir)) @mkdir($domainDir, 0770, true);

        // Generate 2048-bit DKIM Key pair
        $dkimDomainDir = "{$this->dkimDir}/{$domain}";
        if (!is_dir($dkimDomainDir)) @mkdir($dkimDomainDir, 0750, true);

        $privateKey = "{$dkimDomainDir}/default.private";
        $txtRecord = "{$dkimDomainDir}/default.txt";

        if (!file_exists($privateKey)) {
            shell_exec("opendkim-genkey -b 2048 -d " . escapeshellarg($domain) . " -D " . escapeshellarg($dkimDomainDir) . " -s default 2>&1");
            @chown($dkimDomainDir, 'opendkim');
            @chgrp($dkimDomainDir, 'opendkim');
        }

        $dkimPublicKey = '';
        if (file_exists($txtRecord)) {
            $dkimPublicKey = file_get_contents($txtRecord);
        }

        return [
            'domain' => $domain,
            'dkimSelector' => 'default',
            'dkimRecord' => $dkimPublicKey,
            'spfRecord' => "v=spf1 a mx ip4:31.77.8.9 ~all",
            'dmarcRecord' => "v=DMARC1; p=quarantine; rua=mailto:admin@{$domain}"
        ];
    }

    /**
     * Create a virtual mailbox user with hashed password
     */
    public function createMailbox(string $email, string $password, int $quotaMb = 2048): bool
    {
        $parts = explode('@', strtolower(trim($email)));
        if (count($parts) !== 2) return false;

        $user = preg_replace('/[^a-zA-Z0-9_\-\.]/', '', $parts[0]);
        $domain = preg_replace('/[^a-zA-Z0-9_\-\.]/', '', $parts[1]);

        $userDir = "{$this->vmailDir}/{$domain}/{$user}";
        if (!is_dir($userDir)) {
            @mkdir("{$userDir}/cur", 0700, true);
            @mkdir("{$userDir}/new", 0700, true);
            @mkdir("{$userDir}/tmp", 0700, true);
            @chown("{$this->vmailDir}/{$domain}", 'vmail');
            @chgrp("{$this->vmailDir}/{$domain}", 'vmail');
        }

        // Dovecot SHA512-CRYPT password hashing
        $hash = trim(shell_exec("doveadm pw -s SHA512-CRYPT -p " . escapeshellarg($password)) ?? '');

        // Store virtual mailbox entry
        $vmailboxFile = "{$this->postfixDir}/vmailbox";
        $line = "{$email} {$domain}/{$user}/\n";
        file_put_contents($vmailboxFile, $line, FILE_APPEND);
        shell_exec("postmap {$vmailboxFile} 2>/dev/null || true");

        return true;
    }
}
