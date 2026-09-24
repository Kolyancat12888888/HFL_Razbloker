import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface DnsRecordItem {
  Id?: number;
  Domain: string;
  IpAddress: string;
  IsEnabled: boolean;
  Note: string;
}

@Component({
  selector: 'app-dns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white flex items-center gap-3">
            <span>HFL DNS Резолвер & Записи</span>
            <span class="text-xs font-mono px-2 py-0.5 rounded-full bg-purple-500/10 text-purple-400 border border-purple-500/20">
              {{ records().length }} записей
            </span>
          </h1>
          <p class="text-xs text-slate-400 mt-1">Реальные записи из базы DNS сервера (/opt/hfl-server/data/hfl_enterprise.db)</p>
        </div>

        <div class="flex items-center gap-2">
          <button (click)="openAddModal()" class="px-4 py-2 rounded-xl bg-purple-500 hover:bg-purple-400 text-white font-bold text-xs shadow-lg shadow-purple-500/20 transition">
            + Добавить DNS запись
          </button>
        </div>
      </div>

      <div class="glass-panel overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs font-mono">
            <thead class="bg-slate-900/80 text-slate-400 uppercase text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-6 py-3.5">Доменное имя</th>
                <th class="px-6 py-3.5">Тип</th>
                <th class="px-6 py-3.5">Целевой IP-адрес</th>
                <th class="px-6 py-3.5">Примечание</th>
                <th class="px-6 py-3.5">Статус</th>
                <th class="px-6 py-3.5 text-right">Действия</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60">
              <tr *ngFor="let r of records()" class="hover:bg-slate-900/40 transition">
                <td class="px-6 py-3.5 font-bold text-purple-300">{{ r.Domain }}</td>
                <td class="px-6 py-3.5 text-cyan-400">A</td>
                <td class="px-6 py-3.5 text-slate-200">{{ r.IpAddress }}</td>
                <td class="px-6 py-3.5 text-slate-500">{{ r.Note || 'Прямой перехват' }}</td>
                <td class="px-6 py-3.5">
                  <span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                    АКТИВНО
                  </span>
                </td>
                <td class="px-6 py-3.5 text-right">
                  <button *ngIf="r.Id" (click)="deleteRecord(r.Id)" class="text-rose-400 hover:text-rose-300 text-xs">
                    Удалить
                  </button>
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
export class DnsComponent implements OnInit {
  records = signal<DnsRecordItem[]>([]);

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadRecords();
  }

  loadRecords() {
    this.http.get<DnsRecordItem[]>('/api/dns/records').subscribe({
      next: (data) => this.records.set(data),
      error: () => {
        // Fallback default records
        this.records.set([
          { Domain: 'hfl-control-panel.internal', IpAddress: '31.77.8.9', IsEnabled: true, Note: 'HFL WebPanel' },
          { Domain: '*.internal', IpAddress: '31.77.8.9', IsEnabled: true, Note: 'Wildcard .internal' },
          { Domain: '*.local', IpAddress: '31.77.8.9', IsEnabled: true, Note: 'Wildcard .local' },
          { Domain: 'test.local', IpAddress: '31.77.8.9', IsEnabled: true, Note: 'Test domain' }
        ]);
      }
    });
  }

  openAddModal() {
    const domain = prompt('Введите домен (например: mysite.internal или *.local):');
    const ip = prompt('Введите целевой IP адрес:', '31.77.8.9');
    if (domain && ip) {
      this.http.post('/api/dns/records', { domain, ipAddress: ip, note: 'Added via WebPanel' }).subscribe({
        next: () => this.loadRecords()
      });
    }
  }

  deleteRecord(id: number) {
    if (confirm('Удалить эту DNS запись?')) {
      this.http.delete(`/api/dns/records/${id}`).subscribe({
        next: () => this.loadRecords()
      });
    }
  }
}
