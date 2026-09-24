import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">Почтовый сервер (Postfix / IMAP)</h1>
          <p class="text-xs text-slate-400 mt-1">Управление почтовыми ящиками, DKIM, SPF, DMARC и Webmail</p>
        </div>
        <button class="px-4 py-2 rounded-xl bg-blue-500 hover:bg-blue-400 text-white font-bold text-xs shadow-lg shadow-blue-500/20 transition">
          + Создать ящик
        </button>
      </div>

      <div class="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Активные почтовые ящики</h2>
        <div class="space-y-3 font-mono text-xs">
          <div class="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
            <div>
              <div class="font-bold text-blue-400 text-sm">admin&#64;hfl-nodes.pro</div>
              <div class="text-slate-500 mt-0.5">Квота: 2.1 GB / 10 GB &bull; SPF, DKIM Active</div>
            </div>
            <button class="text-cyan-400 hover:underline">Roundcube Webmail ↗</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class MailComponent {}
