import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemMonitorService, ServerStat, ServiceStatus } from '../../services/system-monitor.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-8 animate-fade-in">
      <!-- Welcome Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight bg-gradient-to-r from-white via-slate-200 to-slate-400 bg-clip-text text-transparent flex items-center gap-3">
            <span>Панель управления сервером</span>
            <span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">LIVE DATA</span>
          </h1>
          <p class="text-sm text-slate-400 mt-1">
            Комплексный мониторинг служб, ресурсов и сетевого шлюза HFL Enterprise
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button (click)="restartService('nginx')" class="btn-action">
            <svg class="w-3.5 h-3.5 text-cyan-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
            <span>Перезапустить Nginx</span>
          </button>
          <button (click)="restartService('hfl-server')" class="btn-action">
            <svg class="w-3.5 h-3.5 text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
            <span>Перезапустить DNS</span>
          </button>
        </div>
      </div>

      <!-- Resource Metrics Grid -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        <!-- CPU Card -->
        <div class="stat-card group">
          <div class="flex items-center justify-between">
            <span class="text-xs font-mono uppercase tracking-wider text-slate-400 font-semibold">Процессор (CPU)</span>
            <div class="w-8 h-8 rounded-lg bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center text-cyan-400 group-hover:scale-110 transition">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"/>
              </svg>
            </div>
          </div>
          <div class="mt-4 flex items-baseline gap-2">
            <span class="text-3xl font-extrabold font-mono text-cyan-400">{{ stats().cpuUsage }}%</span>
            <span class="text-xs text-slate-500 font-mono">/proc/stat</span>
          </div>
          <div class="w-full bg-slate-800/80 rounded-full h-1.5 mt-3 overflow-hidden">
            <div class="bg-gradient-to-r from-cyan-500 to-sky-400 h-1.5 rounded-full transition-all duration-500" [style.width.%]="stats().cpuUsage"></div>
          </div>
        </div>

        <!-- RAM Card -->
        <div class="stat-card group">
          <div class="flex items-center justify-between">
            <span class="text-xs font-mono uppercase tracking-wider text-slate-400 font-semibold">Оперативная память</span>
            <div class="w-8 h-8 rounded-lg bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center text-indigo-400 group-hover:scale-110 transition">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/>
              </svg>
            </div>
          </div>
          <div class="mt-4 flex items-baseline gap-2">
            <span class="text-3xl font-extrabold font-mono text-indigo-400">{{ stats().ramUsage }}%</span>
            <span class="text-xs text-slate-500 font-mono">{{ stats().ramUsed }} / {{ stats().ramTotal }}</span>
          </div>
          <div class="w-full bg-slate-800/80 rounded-full h-1.5 mt-3 overflow-hidden">
            <div class="bg-gradient-to-r from-indigo-500 to-purple-400 h-1.5 rounded-full transition-all duration-500" [style.width.%]="stats().ramUsage"></div>
          </div>
        </div>

        <!-- Disk Card -->
        <div class="stat-card group">
          <div class="flex items-center justify-between">
            <span class="text-xs font-mono uppercase tracking-wider text-slate-400 font-semibold">Дисковое пространство</span>
            <div class="w-8 h-8 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400 group-hover:scale-110 transition">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4"/>
              </svg>
            </div>
          </div>
          <div class="mt-4 flex items-baseline gap-2">
            <span class="text-3xl font-extrabold font-mono text-emerald-400">{{ stats().diskUsage }}%</span>
            <span class="text-xs text-slate-500 font-mono">{{ stats().diskUsed }} / {{ stats().diskTotal }}</span>
          </div>
          <div class="w-full bg-slate-800/80 rounded-full h-1.5 mt-3 overflow-hidden">
            <div class="bg-gradient-to-r from-emerald-500 to-teal-400 h-1.5 rounded-full transition-all duration-500" [style.width.%]="stats().diskUsage"></div>
          </div>
        </div>

        <!-- Sites & DBs Card -->
        <div class="stat-card group">
          <div class="flex items-center justify-between">
            <span class="text-xs font-mono uppercase tracking-wider text-slate-400 font-semibold">Сайты и Базы</span>
            <div class="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400 group-hover:scale-110 transition">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
              </svg>
            </div>
          </div>
          <div class="mt-4 flex items-baseline gap-2">
            <span class="text-3xl font-extrabold font-mono text-purple-400">{{ stats().activeSites }}</span>
            <span class="text-xs text-slate-500 font-mono">сайтов &bull; {{ stats().activeDbs }} БД</span>
          </div>
          <div class="w-full bg-slate-800/80 rounded-full h-1.5 mt-3 overflow-hidden">
            <div class="bg-gradient-to-r from-purple-500 to-pink-400 h-1.5 rounded-full" style="width: 100%"></div>
          </div>
        </div>
      </div>

      <!-- Services & Gateway Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- System Services Status -->
        <div class="lg:col-span-2 glass-panel p-6">
          <div class="flex items-center justify-between mb-5">
            <div class="flex items-center gap-2.5">
              <div class="w-2.5 h-2.5 rounded-full bg-cyan-400 animate-ping"></div>
              <h2 class="text-base font-bold text-slate-100">Системные службы сервера (systemctl)</h2>
            </div>
            <button (click)="loadServices()" class="text-xs font-mono text-cyan-400 hover:underline">Обновить</button>
          </div>

          <div class="space-y-3">
            <div *ngFor="let s of services()" class="flex items-center justify-between p-3.5 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-slate-700 transition">
              <div class="flex items-center gap-3.5">
                <div class="w-2.5 h-2.5 rounded-full" [ngClass]="s.status === 'running' ? 'bg-emerald-400 shadow-lg shadow-emerald-400/50' : 'bg-rose-500'"></div>
                <div>
                  <div class="text-sm font-semibold text-slate-200 flex items-center gap-2">
                    <span>{{ s.name }}</span>
                    <span class="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">{{ s.port }}</span>
                  </div>
                  <div class="text-xs text-slate-500">{{ s.description }}</div>
                </div>
              </div>

              <div class="flex items-center gap-2">
                <span class="text-xs font-mono px-2 py-0.5 rounded border"
                  [ngClass]="s.status === 'running' ? 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20' : 'text-rose-400 bg-rose-500/10 border-rose-500/20'">
                  {{ s.status === 'running' ? 'Active' : 'Inactive' }}
                </span>
                <button (click)="restartService(s.name)" class="p-1.5 rounded-lg bg-slate-800/60 hover:bg-slate-700 text-slate-400 hover:text-slate-200 transition">
                  <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- HFL Gateway & DNS Info -->
        <div class="glass-panel p-6 flex flex-col justify-between">
          <div>
            <div class="flex items-center gap-2.5 mb-4">
              <div class="w-2 h-2 rounded-full bg-purple-400"></div>
              <h2 class="text-base font-bold text-slate-100">Шлюз и DNS Маршрутизация</h2>
            </div>

            <div class="space-y-3.5 text-xs">
              <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800/80">
                <div class="text-slate-400 uppercase text-[10px] font-mono font-semibold">Публичный IP шлюза</div>
                <div class="text-sm font-bold font-mono text-cyan-400 mt-0.5">31.77.8.9</div>
              </div>

              <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800/80">
                <div class="text-slate-400 uppercase text-[10px] font-mono font-semibold">DNS Зоны перехвата</div>
                <div class="text-sm font-bold font-mono text-purple-400 mt-0.5">*.local &bull; *.internal</div>
              </div>

              <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800/80">
                <div class="text-slate-400 uppercase text-[10px] font-mono font-semibold">Автоматические SSL</div>
                <div class="text-sm font-bold font-mono text-emerald-400 mt-0.5">X.509 RSA-2048 SAN</div>
              </div>
            </div>
          </div>

          <div class="mt-6 pt-4 border-t border-slate-800/80 flex items-center justify-between text-xs text-slate-500 font-mono">
            <span>Uptime: {{ stats().uptime }}</span>
            <span class="text-cyan-400">v1.0.0 Enterprise</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stat-card {
      background: rgba(15, 23, 42, 0.7);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(56, 189, 248, 0.12);
      border-radius: 1rem;
      padding: 1.25rem;
      transition: all 0.25s ease;
    }
    .stat-card:hover {
      border-color: rgba(56, 189, 248, 0.3);
      transform: translateY(-2px);
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.4);
    }
    .glass-panel {
      background: rgba(15, 23, 42, 0.75);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 1.25rem;
    }
    .btn-action {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 0.875rem;
      border-radius: 0.75rem;
      background: rgba(30, 41, 59, 0.6);
      border: 1px solid rgba(255, 255, 255, 0.1);
      font-size: 0.75rem;
      font-weight: 600;
      color: #cbd5e1;
      transition: all 0.2s ease;
    }
    .btn-action:hover {
      background: rgba(51, 65, 85, 0.8);
      color: #ffffff;
      border-color: rgba(56, 189, 248, 0.3);
    }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats = signal<ServerStat>({
    cpuUsage: 0,
    ramUsage: 0,
    ramTotal: 'Загрузка...',
    ramUsed: '0 GB',
    diskUsage: 0,
    diskTotal: 'Загрузка...',
    diskUsed: '0 GB',
    uptime: 'Загрузка...',
    activeSites: 0,
    activeDbs: 0,
    dnsQueriesTotal: 0
  });

  services = signal<ServiceStatus[]>([]);
  private pollSub?: Subscription;

  constructor(private monitorService: SystemMonitorService) {}

  ngOnInit() {
    this.pollSub = this.monitorService.getLiveMetricsPolling(2000).subscribe({
      next: (data) => this.stats.set(data),
      error: () => {}
    });

    this.loadServices();
  }

  loadServices() {
    this.monitorService.getServicesStatus().subscribe({
      next: (data) => this.services.set(data),
      error: () => {}
    });
  }

  restartService(name: string) {
    this.monitorService.restartService(name).subscribe({
      next: () => {
        this.loadServices();
      }
    });
  }

  ngOnDestroy() {
    this.pollSub?.unsubscribe();
  }
}
