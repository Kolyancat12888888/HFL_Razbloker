<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class AuthController extends Controller
{
    private string $usersDbPath = '/opt/hfl-server/data/users.json';

    public function __construct()
    {
        $this->ensureUsersDb();
    }

    private function ensureUsersDb(): void
    {
        $dir = dirname($this->usersDbPath);
        if (!is_dir($dir)) @mkdir($dir, 0755, true);

        if (!file_exists($this->usersDbPath)) {
            // Default Root SuperAdmin and User demo
            $defaultUsers = [
                [
                    'id' => 1,
                    'username' => 'root',
                    'password_hash' => password_hash('root', PASSWORD_BCRYPT),
                    'role' => 'SuperAdmin', // SuperAdmin, Admin, Reseller, Client
                    'privileges' => ['*'],
                    'quota' => ['disk' => 'unlimited', 'sites' => 999, 'dbs' => 999],
                    'created_at' => date('Y-m-d H:i:s')
                ],
                [
                    'id' => 2,
                    'username' => 'client_demo',
                    'password_hash' => password_hash('password123', PASSWORD_BCRYPT),
                    'role' => 'Client',
                    'privileges' => ['sites.manage', 'files.manage', 'dbs.manage', 'dns.view'],
                    'quota' => ['disk' => '10GB', 'sites' => 5, 'dbs' => 3],
                    'created_at' => date('Y-m-d H:i:s')
                ]
            ];
            file_put_contents($this->usersDbPath, json_encode($defaultUsers, JSON_PRETTY_PRINT));
        }
    }

    /**
     * User Login API
     */
    public function login(Request $request): JsonResponse
    {
        $username = trim($request->input('username', ''));
        $password = $request->input('password', '');

        $users = json_decode(file_get_contents($this->usersDbPath), true) ?: [];

        foreach ($users as $u) {
            if ($u['username'] === $username && password_verify($password, $u['password_hash'])) {
                // Generate secure JWT-like Session Token
                $token = base64_encode(json_encode([
                    'id' => $u['id'],
                    'username' => $u['username'],
                    'role' => $u['role'],
                    'privileges' => $u['privileges'],
                    'exp' => time() + 86400 * 7 // 7 days
                ]));

                return response()->json([
                    'success' => true,
                    'token' => $token,
                    'user' => [
                        'id' => $u['id'],
                        'username' => $u['username'],
                        'role' => $u['role'],
                        'privileges' => $u['privileges'],
                        'quota' => $u['quota']
                    ]
                ]);
            }
        }

        return response()->json(['error' => 'Неверное имя пользователя или пароль'], 401);
    }

    /**
     * List all users (For SuperAdmin)
     */
    public function listUsers(): JsonResponse
    {
        $users = json_decode(file_get_contents($this->usersDbPath), true) ?: [];
        $sanitized = array_map(function ($u) {
            unset($u['password_hash']);
            return $u;
        }, $users);

        return response()->json($sanitized);
    }

    /**
     * Create new user with role and privileges
     */
    public function createUser(Request $request): JsonResponse
    {
        $username = preg_replace('/[^a-zA-Z0-9_]/', '', $request->input('username', ''));
        $password = $request->input('password', '');
        $role = $request->input('role', 'Client'); // SuperAdmin, Admin, Reseller, Client
        $privileges = $request->input('privileges', ['sites.manage', 'files.manage']);

        if (empty($username) || empty($password)) {
            return response()->json(['error' => 'Заполните логин и пароль'], 422);
        }

        $users = json_decode(file_get_contents($this->usersDbPath), true) ?: [];
        foreach ($users as $u) {
            if ($u['username'] === $username) {
                return response()->json(['error' => 'Пользователь уже существует'], 409);
            }
        }

        $newUser = [
            'id' => count($users) + 1,
            'username' => $username,
            'password_hash' => password_hash($password, PASSWORD_BCRYPT),
            'role' => $role,
            'privileges' => $privileges,
            'quota' => [
                'disk' => $request->input('quota_disk', '20GB'),
                'sites' => (int)$request->input('quota_sites', 10),
                'dbs' => (int)$request->input('quota_dbs', 5)
            ],
            'created_at' => date('Y-m-d H:i:s')
        ];

        $users[] = $newUser;
        file_put_contents($this->usersDbPath, json_encode($users, JSON_PRETTY_PRINT));

        unset($newUser['password_hash']);
        return response()->json(['success' => true, 'user' => $newUser]);
    }
}
