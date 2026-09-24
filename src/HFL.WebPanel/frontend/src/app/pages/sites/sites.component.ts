import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface WebSite {
  id: number;
  domain: string;
  aliases: string[];
  docRoot: string;
  phpVersion: string;
  sslActive: boolean;
  sslType: string;
  status: 'active' | 'suspended';
  envType: 'PHP' | 'Node.js' | 'Python' | 'Docker' | 'Static';
  port?: number;
}

@Component({
  selector: 'app-sites',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <!-- Header with Action -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white flex items-center gap-3">
            <span>Сайты и Веб-серверы</span>
            <span class="text-xs font-mono px-2.5 py-1 rounded-full bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
              {{ sites().length }} активных
            </span>
          </h1>
          <p class="text-xs text-slate-400 mt-1">Управление виртуальными хостами Nginx, PHP-FPM, Node.js, Python и автовыпуск SSL</p>
        </div>

        <button (click)="openAddModal()" class="px-4 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-sky-500 hover:from-cyan-400 hover:to-sky-400 text-dark-950 font-bold text-xs flex items-center gap-2 shadow-lg shadow-cyan-500/20 transition transform active:scale-95">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          <span>Создать сайт</span>
        </button>
      </div>

      <!-- Sites Table -->
      <div class="glass-panel overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead class="bg-slate-900/80 text-slate-400 uppercase font-mono text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-6 py-4">Домен / Алиасы</th>
                <th class="px-6 py-4">Окружение</th>
                <th class="px-6 py-4">Корневая папка</th>
                <th class="px-6 py-4">SSL Сертификат</th>
                <th class="px-6 py-4">Статус</th>
                <th class="px-6 py-4 text-right">Действия</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60">
              <tr *ngFor="let site of sites()" class="hover:bg-slate-900/40 transition">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <div class="w-8 h-8 rounded-lg bg-sky-500/10 border border-sky-500/20 flex items-center justify-center text-sky-400 font-bold font-mono">
                      {{ site.domain[0].toUpperCase() }}
                    </div>
                    <div>
                      <a [href]="'https://' + site.domain" target="_blank" class="font-bold text-slate-200 hover:text-cyan-400 transition flex items-center gap-1.5">
                        <span>{{ site.domain }}</span>
                        <svg class="w-3 h-3 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/>
                        </svg>
                      </a>
                      <div class="text-[10px] text-slate-500 font-mono mt-0.5">
                        {{ site.aliases.join(', ') || 'Без алиасов' }}
                      </div>
                    </div>
                  </div>
                </td>

                <td class="px-6 py-4">
                  <span class="px-2.5 py-1 rounded-md font-mono text-[10px] font-semibold"
                    [ngClass]="{
                      'bg-sky-500/10 text-sky-400 border border-sky-500/20': site.envType === 'PHP',
                      'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20': site.envType === 'Node.js',
                      'bg-amber-500/10 text-amber-400 border border-amber-500/20': site.envType === 'Python',
                      'bg-purple-500/10 text-purple-400 border border-purple-500/20': site.envType === 'Docker'
                    }">
                    {{ site.envType }} {{ site.phpVersion }}
                  </span>
                </td>

                <td class="px-6 py-4 font-mono text-slate-400 text-[11px] truncate max-w-xs">
                  {{ site.docRoot }}
                </td>

                <td class="px-6 py-4">
                  <div class="flex items-center gap-1.5 text-emerald-400 font-mono text-[11px]">
                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                    </svg>
                    <span>{{ site.sslType }}</span>
                  </div>
                </td>

                <td class="px-6 py-4">
                  <span class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-mono font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                    <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                    <span>ONLINE</span>
                  </span>
                </td>

                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button title="Настройки Nginx" class="p-1.5 rounded-lg bg-slate-800/60 hover:bg-slate-700 text-slate-400 hover:text-cyan-400 transition">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                      </svg>
                    </button>
                    <button title="Файлы сайта" class="p-1.5 rounded-lg bg-slate-800/60 hover:bg-slate-700 text-slate-400 hover:text-emerald-400 transition">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"/>
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .glass-panel {
      background: rgba(15, 23, 42, 0.75);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 1rem;
    }
  `]
})
export class SitesComponent {
  sites = signal<WebSite[]>([
    {
      id: 1,
      domain: 'test.local',
      aliases: ['*.local'],
      docRoot: '/var/www/hfl-local',
      phpVersion: '8.3',
      sslActive: true,
      sslType: 'HFL SAN SSL',
      status: 'active',
      envType: 'PHP'
    },
    {
      id: 2,
      domain: 'panel.local',
      aliases: ['panel.internal'],
      docRoot: '/var/www/hfl-panel',
      phpVersion: '8.3',
      sslActive: true,
      sslType: 'HFL SAN SSL',
      status: 'active',
      envType: 'PHP'
    },
    {
      id: 3,
      domain: 'cs2.hfl-nodes.pro',
      aliases: ['www.cs2.hfl-nodes.pro'],
      docRoot: '/var/www/cs2panel',
      phpVersion: '8.2',
      sslActive: true,
      sslType: 'Let\'s Encrypt',
      status: 'active',
      envType: 'PHP'
    }
  ]);

  openAddModal() {
    alert('Мастер создания сайтов открыт: выберите домен (.local / .com), PHP/Node.js версию и SSL.');
  }
}
