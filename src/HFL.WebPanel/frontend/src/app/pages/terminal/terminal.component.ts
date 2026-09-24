import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-terminal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white flex items-center gap-3">
            <span>Web-Терминал (SSH / Root Console)</span>
            <span class="text-xs font-mono px-2 py-0.5 rounded bg-lime-500/10 text-lime-400 border border-lime-500/20">ROOT ACTIVE</span>
          </h1>
          <p class="text-xs text-slate-400 mt-1">Прямой доступ к командной строке сервера</p>
        </div>
      </div>

      <div class="p-6 rounded-2xl bg-dark-950 border border-slate-800 shadow-2xl font-mono text-xs text-lime-400 min-h-[450px] flex flex-col justify-between">
        <div class="space-y-2">
          <div class="text-slate-500">Linux advisory-moccasin-cattle 6.8.0-40-generic x86_64</div>
          <div class="text-slate-500">HFL Enterprise Shell v1.0.0 (Type 'help' for quick commands)</div>
          <div class="mt-4"><span class="text-cyan-400 font-bold">root&#64;advisory-moccasin-cattle:~#</span> uname -a</div>
          <div class="text-slate-300">Linux advisory-moccasin-cattle 6.8.0-40-generic #40-Ubuntu SMP PREEMPT_DYNAMIC Thu Jun 13 18:01:26 UTC 2024 x86_64 GNU/Linux</div>
          <div class="mt-2"><span class="text-cyan-400 font-bold">root&#64;advisory-moccasin-cattle:~#</span> systemctl is-active hfl-server nginx</div>
          <div class="text-emerald-400">active</div>
          <div class="text-emerald-400">active</div>
        </div>

        <div class="flex items-center gap-2 pt-4 border-t border-slate-900 mt-4">
          <span class="text-cyan-400 font-bold">root&#64;advisory-moccasin-cattle:~#</span>
          <input type="text" placeholder="Введите команду (например: systemctl status hfl-server)..." class="flex-1 bg-transparent text-slate-100 focus:outline-none text-xs">
        </div>
      </div>
    </div>
  `
})
export class TerminalComponent {}
