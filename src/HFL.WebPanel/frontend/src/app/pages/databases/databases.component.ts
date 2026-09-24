import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-databases',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">Базы данных & СУБД</h1>
          <p class="text-xs text-slate-400 mt-1">Управление MySQL 8, MariaDB, PostgreSQL, пользователями и дампами</p>
        </div>
        <button class="px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-dark-950 font-bold text-xs shadow-lg shadow-emerald-500/20 transition">
          + Создать базу данных
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div *ngFor="let db of dbs()" class="p-5 rounded-2xl bg-slate-900/60 border border-slate-800/80 hover:border-emerald-500/30 transition">
          <div class="flex items-center justify-between">
            <span class="text-xs font-mono font-bold text-emerald-400">{{ db.engine }}</span>
            <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400">{{ db.size }}</span>
          </div>
          <div class="text-base font-bold text-slate-200 mt-2">{{ db.name }}</div>
          <div class="text-xs text-slate-500 font-mono mt-0.5">Пользователь: {{ db.user }}</div>
          <div class="mt-4 pt-3 border-t border-slate-800 flex items-center justify-between text-xs">
            <button class="text-emerald-400 hover:underline">phpMyAdmin ↗</button>
            <button class="text-slate-400 hover:text-slate-200">Дамп .SQL</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DatabasesComponent {
  dbs = signal([
    { name: 'hfl_enterprise_db', engine: 'MySQL 8.4', user: 'hfl_admin', size: '14.2 MB' },
    { name: 'cs2_panel_prod', engine: 'MariaDB 10.11', user: 'cs2_user', size: '86.4 MB' },
    { name: 'panel_laravel_db', engine: 'PostgreSQL 16', user: 'postgres_app', size: '5.1 MB' }
  ]);
}
