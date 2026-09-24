import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-ssl',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white">SSL / TLS Сертификаты</h1>
          <p class="text-xs text-slate-400 mt-1">Криптографические сертификаты X.509 RSA-2048 SAN для локальных и внешних доменов</p>
        </div>
        <a href="/api/v1/certificates/root-ca" download class="px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-dark-950 font-bold text-xs shadow-lg shadow-amber-500/20 transition">
          📥 Скачать Root CA (.crt)
        </a>
      </div>

      <div class="p-6 rounded-2xl bg-slate-900/60 border border-slate-800">
        <h2 class="text-sm font-bold text-slate-200 mb-4 font-mono uppercase">Активные SSL сертификаты</h2>
        <div class="space-y-3">
          <div *ngFor="let c of certs()" class="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between">
            <div>
              <div class="font-bold text-slate-200 text-sm font-mono">{{ c.domain }}</div>
              <div class="text-xs text-slate-500 font-mono mt-0.5">{{ c.issuer }} &bull; Действителен до: {{ c.expires }}</div>
            </div>
            <span class="px-2.5 py-1 rounded text-xs font-mono font-bold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
              TRUSTED
            </span>
          </div>
        </div>
      </div>
    </div>
  `
})
export class SslComponent {
  certs = signal([
    { domain: '*.local, *.internal, test.local, panel.local', issuer: 'CN=HFL Razbloker Root CA', expires: '2036-09-24' },
    { domain: 'cs2.hfl-nodes.pro', issuer: 'Let\'s Encrypt Authority X3', expires: '2026-12-24' }
  ]);
}
