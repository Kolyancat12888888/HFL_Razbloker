import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white flex items-center gap-3">
            <span>Безопасность & Файрвол UFW</span>
            <span class="text-xs font-mono px-2.5 py-1 rounded-full"
              [ngClass]="ufwActive() ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-rose-500/10 text-rose-400 border border-rose-500/20'">
              {{ ufwActive() ? 'UFW ACTIVE' : 'UFW INACTIVE' }}
            </span>
          </h1>
          <p class="text-xs text-slate-400 mt-1">Управление портами сервера, Fail2ban защита от брутфорса и анти-DDoS</p>
        </div>

        <div class="flex items-center gap-2">
          <input [(ngModel)]="newPort" type="number" placeholder="Порт (напр. 8080)" class="bg-slate-900 border border-slate-700 text-xs px-3 py-2 rounded-xl text-slate-200 focus:outline-none focus:border-cyan-500">
          <button (click)="openPort()" class="px-4 py-2 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-dark-950 font-bold text-xs shadow-lg shadow-cyan-500/20 transition">
            + Открыть порт
          </button>
        </div>
      </div>

      <!-- Firewall Rules Table -->
      <div class="glass-panel p-6">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Активные правила UFW</h2>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs font-mono">
            <thead class="bg-slate-900 text-slate-400 text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-4 py-3">Порт / Протокол</th>
                <th class="px-4 py-3">Действие</th>
                <th class="px-4 py-3">Источник</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60">
              <tr *ngFor="let r of rules()" class="hover:bg-slate-900/40">
                <td class="px-4 py-3 font-bold text-cyan-400">{{ r.to }}</td>
                <td class="px-4 py-3 text-emerald-400">{{ r.action }}</td>
                <td class="px-4 py-3 text-slate-400">{{ r.from }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Fail2ban Jails -->
      <div class="glass-panel p-6">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Активные джейлы Fail2ban</h2>
        <div class="flex flex-wrap gap-3">
          <div *ngFor="let jail of jails()" class="px-3 py-2 rounded-xl bg-slate-900/80 border border-slate-800 flex items-center gap-2 text-xs font-mono text-slate-300">
            <span class="w-2 h-2 rounded-full bg-emerald-400"></span>
            <span>{{ jail }}</span>
          </div>
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
export class SecurityComponent implements OnInit {
  ufwActive = signal<boolean>(true);
  rules = signal<any[]>([]);
  jails = signal<string[]>([]);
  newPort: number = 8080;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadSecurity();
  }

  loadSecurity() {
    this.http.get<any>('/api/security/firewall').subscribe({
      next: (data) => {
        this.ufwActive.set(data.ufwActive);
        this.rules.set(data.rules);
        this.jails.set(data.fail2banJails);
      }
    });
  }

  openPort() {
    if (this.newPort) {
      this.http.post('/api/security/firewall/port', { port: this.newPort, action: 'allow' }).subscribe({
        next: () => this.loadSecurity()
      });
    }
  }
}
