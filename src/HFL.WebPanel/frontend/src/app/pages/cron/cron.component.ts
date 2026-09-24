import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface CronJob {
  id: number;
  schedule: string;
  command: string;
  enabled: boolean;
}

@Component({
  selector: 'app-cron',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">Планировщик задач (Crontab)</h1>
          <p class="text-xs text-slate-400 mt-1">Реальное системное расписание задач Linux</p>
        </div>
        <button (click)="openAddModal()" class="px-4 py-2 rounded-xl bg-teal-500 hover:bg-teal-400 text-dark-950 font-bold text-xs shadow-lg shadow-teal-500/20 transition">
          + Новая задача Cron
        </button>
      </div>

      <div class="glass-panel overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs font-mono">
            <thead class="bg-slate-900 text-slate-400 text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-6 py-3.5">Расписание</th>
                <th class="px-6 py-3.5">Команда</th>
                <th class="px-6 py-3.5">Статус</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60">
              <tr *ngFor="let job of jobs()" class="hover:bg-slate-900/40">
                <td class="px-6 py-3.5 font-bold text-teal-400">{{ job.schedule }}</td>
                <td class="px-6 py-3.5 text-slate-200">{{ job.command }}</td>
                <td class="px-6 py-3.5">
                  <span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">АКТИВНО</span>
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
export class CronComponent implements OnInit {
  jobs = signal<CronJob[]>([]);

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadJobs();
  }

  loadJobs() {
    this.http.get<CronJob[]>('/api/cron/list').subscribe({
      next: (data) => this.jobs.set(data)
    });
  }

  openAddModal() {
    const cmd = prompt('Введите команду для запуска:');
    const sch = prompt('Введите расписание cron (например 0 * * * *):', '0 * * * *');
    if (cmd && sch) {
      this.http.post('/api/cron/create', { schedule: sch, command: cmd }).subscribe({
        next: () => this.loadJobs()
      });
    }
  }
}
