import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">Пользователи & Квоты (Multi-Tenancy)</h1>
          <p class="text-xs text-slate-400 mt-1">Системные пользователи Linux, изоляция сайтов и лимиты</p>
        </div>
        <button class="px-4 py-2 rounded-xl bg-pink-500 hover:bg-pink-400 text-white font-bold text-xs shadow-lg shadow-pink-500/20 transition">
          + Добавить пользователя
        </button>
      </div>

      <div class="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Активные аккаунты</h2>
        <div class="space-y-3 font-mono text-xs">
          <div *ngFor="let u of users()" class="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
            <div>
              <div class="font-bold text-pink-400 text-sm">{{ u.username }} ({{ u.role }})</div>
              <div class="text-slate-500 mt-0.5">Дисковая квота: {{ u.disk }} &bull; Сайтов: {{ u.sites }} &bull; Баз: {{ u.dbs }}</div>
            </div>
            <button class="text-cyan-400 hover:underline">Войти под пользователем ↗</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class UsersComponent {
  users = signal([
    { username: 'root', role: 'Super Administrator', disk: 'Без ограничений', sites: 'Все', dbs: 'Все' },
    { username: 'client_cs2', role: 'Customer', disk: '15.4 GB / 50 GB', sites: '2', dbs: '2' },
    { username: 'dev_user', role: 'Developer', disk: '2.1 GB / 20 GB', sites: '1', dbs: '1' }
  ]);
}
