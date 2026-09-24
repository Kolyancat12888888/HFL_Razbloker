<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\System\DnsManagerService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class DnsRecordsController extends Controller
{
    private DnsManagerService $dnsService;

    public function __construct(DnsManagerService $dnsService)
    {
        $this->dnsService = $dnsService;
    }

    /**
     * Get all DNS records from HFL DNS Database
     */
    public function index(): JsonResponse
    {
        $records = $this->dnsService->listRecords();
        return response()->json($records);
    }

    /**
     * Add or update DNS record in HFL DNS Database
     */
    public function store(Request $request): JsonResponse
    {
        $domain = $request->input('domain');
        $ip = $request->input('ipAddress', '31.77.8.9');
        $note = $request->input('note', 'Added via HFL WebPanel');

        if (empty($domain)) {
            return response()->json(['error' => 'Domain is required'], 422);
        }

        $success = $this->dnsService->addOrUpdateRecord($domain, $ip, $note);
        return response()->json(['success' => $success]);
    }

    /**
     * Delete DNS record by ID
     */
    public function destroy(int $id): JsonResponse
    {
        $success = $this->dnsService->deleteRecord($id);
        return response()->json(['success' => $success]);
    }
}
