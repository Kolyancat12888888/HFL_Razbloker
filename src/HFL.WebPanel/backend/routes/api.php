<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\Api\SystemMonitorController;
use App\Http\Controllers\Api\SiteManagerController;
use App\Http\Controllers\Api\FileManagerController;
use App\Http\Controllers\Api\SecurityManagerController;
use App\Http\Controllers\Api\CronManagerController;

/*
|--------------------------------------------------------------------------
| HFL WebPanel REST API Routes
|--------------------------------------------------------------------------
*/

Route::prefix('system')->group(function () {
    Route::get('/metrics', [SystemMonitorController::class, 'getMetrics']);
    Route::get('/services', [SystemMonitorController::class, 'getServicesStatus']);
    Route::post('/services/restart', [SystemMonitorController::class, 'restartService']);
});

Route::prefix('sites')->group(function () {
    Route::get('/', [SiteManagerController::class, 'index']);
    Route::post('/', [SiteManagerController::class, 'store']);
    Route::delete('/{domain}', [SiteManagerController::class, 'destroy']);
});

Route::prefix('files')->group(function () {
    Route::get('/list', [FileManagerController::class, 'list']);
    Route::get('/read', [FileManagerController::class, 'readFile']);
    Route::post('/save', [FileManagerController::class, 'saveFile']);
    Route::post('/create', [FileManagerController::class, 'createItem']);
    Route::post('/delete', [FileManagerController::class, 'deleteItem']);
});

Route::prefix('security')->group(function () {
    Route::get('/firewall', [SecurityManagerController::class, 'getFirewallRules']);
    Route::post('/firewall/port', [SecurityManagerController::class, 'toggleFirewallPort']);
});

Route::prefix('cron')->group(function () {
    Route::get('/list', [CronManagerController::class, 'index']);
    Route::post('/create', [CronManagerController::class, 'store']);
});
