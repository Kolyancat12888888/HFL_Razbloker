import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-backups',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">Резервное копирование (Backups)</h1>
          <p class="text-xs text-slate-400 mt-1">Создание, скачивание и восстановление копий сайтов и баз данных в 1 клик</p>
        </div>
        <button class="px-4 py-2 rounded-xl bg-indigo-500 hover:bg-indigo-400 text-white font-bold text-xs shadow-lg shadow-indigo-500/20 transition">
          + Создать резервную копию
        </button>
      </div>

      <div class="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Доступные архивы</h2>
        <div class="space-y-3 font-mono text-xs">
          <div *ngFor="let b of backups()" class="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
            <div>
              <div class="font-bold text-indigo-400 text-sm">{{ b.filename }}</div>
              <div class="text-slate-500 mt-0.5">Размер: {{ b.size }} &bull; Дата: {{ b.date }}</div>
            </div>
            <div class="flex items-center gap-3">
              <button class="text-cyan-400 hover:underline">Скачать .tar.gz</button>
              <button class="text-emerald-400 hover:underline">Восстановить</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class BackupsComponent {
  backups = signal([
    { filename: 'backup_full_2026-09-24.tar.gz', size: '1.42 GB', date: '2026-09-24 04:00' },
    { filename: 'backup_mysql_dbs_2026-09-23.sql.gz', size: '124 MB', date: '2026-09-23 04:00' }
  ]);
}
