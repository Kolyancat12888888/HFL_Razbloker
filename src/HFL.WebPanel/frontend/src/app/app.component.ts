import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex h-screen w-full bg-dark-950 text-slate-100 overflow-hidden">
      <!-- Cyberpunk Sidebar -->
      <aside class="w-64 flex-shrink-0 bg-dark-900/90 border-r border-slate-800/80 backdrop-blur-xl flex flex-col justify-between z-20">
        <div>
          <!-- Logo & Brand Header -->
          <div class="h-16 flex items-center px-6 gap-3 border-b border-slate-800/60">
            <div class="w-9 h-9 rounded-xl bg-gradient-to-tr from-cyan-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-cyan-500/20 ring-1 ring-white/20">
              <svg class="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
              </svg>
            </div>
            <div>
              <span class="font-extrabold text-base tracking-wider bg-gradient-to-r from-cyan-400 via-sky-300 to-indigo-400 bg-clip-text text-transparent">HFL CLOUD</span>
              <span class="block text-[10px] text-cyan-400/70 font-mono font-semibold tracking-widest uppercase">ISP ENTERPRISE</span>
            </div>
          </div>

          <!-- Navigation Links -->
          <nav class="p-3 space-y-1 overflow-y-auto max-h-[calc(100vh-140px)]">
            <div class="px-3 py-1.5 text-[10px] font-mono uppercase tracking-wider text-slate-500 font-semibold">Управление сервером</div>
            
            <a routerLink="/" routerLinkActive="active-nav" [routerLinkActiveOptions]="{exact: true}" class="nav-item">
              <svg class="w-4 h-4 text-cyan-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/>
              </svg>
              <span>Дашборд</span>
            </a>

            <a routerLink="/sites" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-sky-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"/>
              </svg>
              <span>Сайты и Домены</span>
              <span class="ml-auto px-1.5 py-0.5 text-[10px] font-mono bg-sky-500/20 text-sky-300 rounded border border-sky-500/30">Nginx</span>
            </a>

            <a routerLink="/databases" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4"/>
              </svg>
              <span>Базы данных</span>
              <span class="ml-auto px-1.5 py-0.5 text-[10px] font-mono bg-emerald-500/20 text-emerald-300 rounded border border-emerald-500/30">SQL</span>
            </a>

            <a routerLink="/dns" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"/>
              </svg>
              <span>HFL DNS Сервер</span>
              <span class="ml-auto px-1.5 py-0.5 text-[10px] font-mono bg-purple-500/20 text-purple-300 rounded border border-purple-500/30">UDP 53</span>
            </a>

            <a routerLink="/ssl" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
              <span>SSL Сертификаты</span>
            </a>

            <a routerLink="/files" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-rose-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"/>
              </svg>
              <span>Файловый менеджер</span>
            </a>

            <div class="pt-3 px-3 py-1.5 text-[10px] font-mono uppercase tracking-wider text-slate-500 font-semibold">Инструменты & DevOps</div>

            <a routerLink="/terminal" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-lime-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
              </svg>
              <span>Web-Терминал</span>
            </a>

            <a routerLink="/cron" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-teal-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              <span>Планировщик Cron</span>
            </a>

            <a routerLink="/mail" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
              </svg>
              <span>Почта (Postfix/IMAP)</span>
            </a>

            <a routerLink="/security" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
              <span>Файрвол & Fail2ban</span>
            </a>

            <a routerLink="/backups" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
              </svg>
              <span>Резервные копии</span>
            </a>

            <a routerLink="/users" routerLinkActive="active-nav" class="nav-item">
              <svg class="w-4 h-4 text-pink-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>
              </svg>
              <span>Пользователи & Квоты</span>
            </a>
          </nav>
        </div>

        <!-- User Profile & Server Status -->
        <div class="p-3 border-t border-slate-800/80 bg-dark-950/40">
          <div class="flex items-center gap-3 p-2 rounded-xl bg-slate-900/60 border border-slate-800">
            <div class="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse shadow-lg shadow-emerald-400/50"></div>
            <div class="flex-1 min-w-0">
              <div class="text-xs font-semibold text-slate-200 truncate">31.77.8.9</div>
              <div class="text-[10px] text-slate-500 font-mono">Ubuntu 24.04 &bull; root</div>
            </div>
            <div class="text-[10px] px-1.5 py-0.5 rounded bg-cyan-500/10 text-cyan-400 font-mono border border-cyan-500/20">v1.0</div>
          </div>
        </div>
      </aside>

      <!-- Main Content Area -->
      <main class="flex-1 flex flex-col min-w-0 overflow-hidden bg-gradient-to-b from-dark-900 to-dark-950">
        <!-- Top Glass Header -->
        <header class="h-16 flex items-center justify-between px-8 border-b border-slate-800/80 backdrop-blur-xl bg-dark-900/50 z-10">
          <div class="flex items-center gap-4">
            <div class="relative w-80">
              <input type="text" placeholder="Быстрый поиск (Ctrl + K)..." class="w-full bg-slate-900/80 border border-slate-800 rounded-xl px-4 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/50 transition">
              <span class="absolute right-3 top-2.5 text-[10px] font-mono text-slate-500 bg-slate-800 px-1.5 py-0.5 rounded border border-slate-700">⌘K</span>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <div class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-xs font-mono">
              <span class="w-2 h-2 rounded-full bg-emerald-400"></span>
              <span>DNS :53 ACTIVE</span>
            </div>

            <div class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-cyan-500/10 border border-cyan-500/20 text-cyan-400 text-xs font-mono">
              <span>Nginx 1.26</span>
            </div>

            <button class="p-2 rounded-xl bg-slate-800/60 border border-slate-700/60 text-slate-400 hover:text-slate-200 transition">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
              </svg>
            </button>
          </div>
        </header>

        <!-- Dynamic Router Outlet with Fade Animation -->
        <div class="flex-1 overflow-y-auto p-8">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      border-radius: 0.75rem;
      font-size: 0.8125rem;
      font-weight: 500;
      color: #94a3b8;
      transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
      border: 1px solid transparent;
    }
    .nav-item:hover {
      color: #f8fafc;
      background-color: rgba(30, 41, 59, 0.5);
      border-color: rgba(56, 189, 248, 0.2);
      transform: translateX(2px);
    }
    .active-nav {
      color: #38bdf8 !important;
      background: linear-gradient(90deg, rgba(56, 189, 248, 0.15) 0%, rgba(56, 189, 248, 0.03) 100%) !important;
      border-color: rgba(56, 189, 248, 0.35) !important;
      box-shadow: 0 0 15px rgba(56, 189, 248, 0.1);
    }
  `]
})
export class AppComponent {}
