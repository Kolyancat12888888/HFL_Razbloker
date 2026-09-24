import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dns',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">HFL DNS Резолвер & Зоны</h1>
          <p class="text-xs text-slate-400 mt-1">Управление записями DNS (A, AAAA, CNAME, TXT, MX) и локальным перехватом</p>
        </div>
        <button class="px-4 py-2 rounded-xl bg-purple-500 hover:bg-purple-400 text-white font-bold text-xs shadow-lg shadow-purple-500/20 transition">
          + Добавить DNS запись
        </button>
      </div>

      <div class="p-6 rounded-2xl bg-slate-900/60 border border-slate-800/80">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead class="bg-slate-900 text-slate-400 uppercase font-mono text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-4 py-3">Доменное имя</th>
                <th class="px-4 py-3">Тип</th>
                <th class="px-4 py-3">Значение / Целевой IP</th>
                <th class="px-4 py-3">TTL</th>
                <th class="px-4 py-3">Перехват</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60 font-mono">
              <tr *ngFor="let r of records()" class="hover:bg-slate-900/40">
                <td class="px-4 py-3 font-bold text-purple-300">{{ r.domain }}</td>
                <td class="px-4 py-3 text-cyan-400">{{ r.type }}</td>
                <td class="px-4 py-3 text-slate-300">{{ r.value }}</td>
                <td class="px-4 py-3 text-slate-500">{{ r.ttl }}s</td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">ACTIVE</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class DnsComponent {
  records = signal([
    { domain: 'test.local', type: 'A', value: '31.77.8.9', ttl: 60 },
    { domain: '*.local', type: 'A', value: '31.77.8.9', ttl: 60 },
    { domain: '*.internal', type: 'A', value: '31.77.8.9', ttl: 60 },
    { domain: 'panel.local', type: 'A', value: '31.77.8.9', ttl: 60 },
    { domain: 'cs2.hfl-nodes.pro', type: 'A', value: '31.77.8.9', ttl: 300 }
  ]);
}
