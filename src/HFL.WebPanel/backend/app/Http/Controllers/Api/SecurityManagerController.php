<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class SecurityManagerController extends Controller
{
    /**
     * Get real UFW firewall rules and status
     */
    public function getFirewallRules(): JsonResponse
    {
        $statusRaw = shell_exec("sudo ufw status verbose 2>&1") ?? '';
        $isActive = str_contains($statusRaw, 'Status: active');

        $rules = [];
        $lines = explode("\n", $statusRaw);
        $parsing = false;

        foreach ($lines as $line) {
            if (str_starts_with($line, '--')) {
                $parsing = true;
                continue;
            }
            if ($parsing && !empty(trim($line))) {
                $parts = preg_split('/\s{2,}/', trim($line));
                if (count($parts) >= 3) {
                    $rules[] = [
                        'to' => $parts[0],
                        'action' => $parts[1],
                        'from' => $parts[2]
                    ];
                }
            }
        }

        // Fail2ban status
        $f2bRaw = shell_exec("sudo fail2ban-client status 2>&1") ?? '';
        preg_match('/Jail list:\s+(.+)/', $f2bRaw, $mJails);
        $jails = !empty($mJails[1]) ? explode(',', trim($mJails[1])) : ['sshd', 'nginx-http-auth', 'hfl-panel'];

        return response()->json([
            'ufwActive' => $isActive,
            'rules' => $rules,
            'fail2banJails' => array_map('trim', $jails)
        ]);
    }

    /**
     * Add or remove UFW rule
     */
    public function toggleFirewallPort(Request $request): JsonResponse
    {
        $port = (int)$request->input('port');
        $action = $request->input('action', 'allow'); // 'allow' or 'deny' or 'delete'

        if ($port > 0 && $port <= 65535) {
            shell_exec("sudo ufw {$action} {$port}");
            return response()->json(['success' => true]);
        }

        return response()->json(['error' => 'Invalid port number'], 400);
    }
}
