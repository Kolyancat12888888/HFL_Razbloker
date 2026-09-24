<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class FileManagerController extends Controller
{
    private string $basePath = '/var/www';

    /**
     * List directory contents (files, folders, sizes, permissions)
     */
    public function list(Request $request): JsonResponse
    {
        $relPath = ltrim($request->input('path', ''), '/');
        $fullPath = realpath($this->basePath . '/' . $relPath);

        if (!$fullPath || !str_starts_with($fullPath, realpath($this->basePath))) {
            $fullPath = realpath($this->basePath);
            $relPath = '';
        }

        $items = [];
        $scan = scandir($fullPath);

        foreach ($scan as $entry) {
            if ($entry === '.') continue;
            if ($entry === '..' && empty($relPath)) continue;

            $itemPath = $fullPath . '/' . $entry;
            $isDir = is_dir($itemPath);
            $size = $isDir ? '-' : $this->formatBytes(filesize($itemPath));
            $perms = substr(sprintf('%o', fileperms($itemPath)), -4);
            $modified = date('Y-m-d H:i:s', filemtime($itemPath));

            $items[] = [
                'name' => $entry,
                'isDir' => $isDir,
                'size' => $size,
                'permissions' => $perms,
                'modified' => $modified,
                'path' => ltrim($relPath . '/' . $entry, '/')
            ];
        }

        return response()->json([
            'currentPath' => $relPath,
            'items' => $items
        ]);
    }

    /**
     * Read text file content for editor (Monaco / Ace)
     */
    public function readFile(Request $request): JsonResponse
    {
        $relPath = ltrim($request->input('path', ''), '/');
        $fullPath = realpath($this->basePath . '/' . $relPath);

        if (!$fullPath || !file_exists($fullPath) || is_dir($fullPath)) {
            return response()->json(['error' => 'File not found'], 404);
        }

        if (filesize($fullPath) > 5 * 1024 * 1024) {
            return response()->json(['error' => 'File is too large for web editor (> 5MB)'], 400);
        }

        $content = file_get_contents($fullPath);
        return response()->json([
            'path' => $relPath,
            'content' => $content,
            'size' => filesize($fullPath)
        ]);
    }

    /**
     * Save text file content from editor
     */
    public function saveFile(Request $request): JsonResponse
    {
        $relPath = ltrim($request->input('path', ''), '/');
        $fullPath = $this->basePath . '/' . $relPath;
        $content = $request->input('content', '');

        file_put_contents($fullPath, $content);
        return response()->json(['success' => true]);
    }

    /**
     * Create new folder or file
     */
    public function createItem(Request $request): JsonResponse
    {
        $path = $this->basePath . '/' . ltrim($request->input('path'), '/');
        $type = $request->input('type', 'file'); // 'file' or 'dir'

        if ($type === 'dir') {
            @mkdir($path, 0755, true);
        } else {
            @touch($path);
        }

        return response()->json(['success' => true]);
    }

    /**
     * Delete file or folder
     */
    public function deleteItem(Request $request): JsonResponse
    {
        $path = realpath($this->basePath . '/' . ltrim($request->input('path'), '/'));

        if ($path && str_starts_with($path, realpath($this->basePath)) && $path !== realpath($this->basePath)) {
            if (is_dir($path)) {
                shell_exec("rm -rf " . escapeshellarg($path));
            } else {
                @unlink($path);
            }
            return response()->json(['success' => true]);
        }

        return response()->json(['error' => 'Invalid path'], 400);
    }

    private function formatBytes(int $bytes): string
    {
        if ($bytes >= 1073741824) return number_format($bytes / 1073741824, 2) . ' GB';
        if ($bytes >= 1048576) return number_format($bytes / 1048576, 2) . ' MB';
        if ($bytes >= 1024) return number_format($bytes / 1024, 2) . ' KB';
        return $bytes . ' B';
    }
}
