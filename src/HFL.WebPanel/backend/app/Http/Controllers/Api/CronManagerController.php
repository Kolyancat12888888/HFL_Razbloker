<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class CronManagerController extends Controller
{
    /**
     * Get real crontab entries
     */
    public function index(): JsonResponse
    {
        $raw = shell_exec("crontab -l 2>/dev/null") ?? '';
        $tasks = [];
        $id = 1;

        $lines = explode("\n", $raw);
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || str_starts_with($line, '#')) continue;

            $parts = explode(' ', $line, 6);
            if (count($parts) === 6) {
                $tasks[] = [
                    'id' => $id++,
                    'schedule' => implode(' ', array_slice($parts, 0, 5)),
                    'command' => $parts[5],
                    'enabled' => true
                ];
            }
        }

        return response()->json($tasks);
    }

    /**
     * Add new Cron job
     */
    public function store(Request $request): JsonResponse
    {
        $schedule = trim($request->input('schedule', '* * * * *'));
        $command = trim($request->input('command', ''));

        if (empty($command)) {
            return response()->json(['error' => 'Command is required'], 422);
        }

        $current = shell_exec("crontab -l 2>/dev/null") ?? '';
        $newCrontab = trim($current) . "\n{$schedule} {$command}\n";

        $tmpFile = tempnam(sys_get_temp_dir(), 'cron');
        file_put_contents($tmpFile, $newCrontab);
        shell_exec("crontab " . escapeshellarg($tmpFile));
        @unlink($tmpFile);

        return response()->json(['success' => true]);
    }
}
