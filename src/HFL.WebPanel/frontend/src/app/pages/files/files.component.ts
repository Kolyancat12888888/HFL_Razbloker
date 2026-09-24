import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface FileItem {
  name: string;
  isDir: boolean;
  size: string;
  permissions: string;
  modified: string;
  path: string;
}

@Component({
  selector: 'app-files',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-extrabold tracking-tight text-white flex items-center gap-3">
            <span>Файловый менеджер</span>
            <span class="text-xs font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400">/var/www/{{ currentPath() }}</span>
          </h1>
          <p class="text-xs text-slate-400 mt-1">Реальный просмотр, редактирование, создание файлов и управление правами</p>
        </div>

        <div class="flex items-center gap-2">
          <button (click)="openNewFileModal()" class="px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-200 border border-slate-700 transition">
            + Новый файл
          </button>
          <button (click)="openNewFolderModal()" class="px-3 py-2 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-xs font-bold text-dark-950 shadow-lg shadow-cyan-500/20 transition">
            + Новая папка
          </button>
        </div>
      </div>

      <!-- File Editor Modal -->
      <div *ngIf="editingFile()" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-dark-950/80 backdrop-blur-md">
        <div class="w-full max-w-4xl bg-dark-900 border border-slate-800 rounded-2xl p-6 shadow-2xl flex flex-col h-[80vh]">
          <div class="flex items-center justify-between pb-4 border-b border-slate-800">
            <div class="font-mono text-sm text-cyan-400 font-bold">{{ editingFile()?.path }}</div>
            <div class="flex items-center gap-2">
              <button (click)="saveFile()" class="px-4 py-1.5 rounded-lg bg-emerald-500 hover:bg-emerald-400 text-dark-950 font-bold text-xs">Сохранить</button>
              <button (click)="editingFile.set(null)" class="px-4 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs">Закрыть</button>
            </div>
          </div>
          <textarea [(ngModel)]="fileContent" class="flex-1 w-full bg-dark-950 text-slate-200 font-mono text-xs p-4 rounded-xl mt-4 border border-slate-800 focus:outline-none focus:border-cyan-500/50 resize-none"></textarea>
        </div>
      </div>

      <!-- Files List -->
      <div class="glass-panel overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs font-mono">
            <thead class="bg-slate-900/80 text-slate-400 uppercase text-[10px] border-b border-slate-800">
              <tr>
                <th class="px-6 py-3.5">Имя</th>
                <th class="px-6 py-3.5">Размер</th>
                <th class="px-6 py-3.5">Права</th>
                <th class="px-6 py-3.5">Дата изменения</th>
                <th class="px-6 py-3.5 text-right">Действия</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60">
              <tr *ngIf="currentPath()" (click)="goUp()" class="hover:bg-slate-900/50 cursor-pointer">
                <td colspan="5" class="px-6 py-3 text-cyan-400 font-bold">📁 .. (Вверх)</td>
              </tr>
              <tr *ngFor="let item of items()" class="hover:bg-slate-900/40 transition">
                <td class="px-6 py-3.5 flex items-center gap-3">
                  <span class="text-base">{{ item.isDir ? '📁' : '📄' }}</span>
                  <button (click)="openItem(item)" class="text-slate-200 hover:text-cyan-400 font-semibold transition text-left">
                    {{ item.name }}
                  </button>
                </td>
                <td class="px-6 py-3.5 text-slate-400">{{ item.size }}</td>
                <td class="px-6 py-3.5 text-slate-500">{{ item.permissions }}</td>
                <td class="px-6 py-3.5 text-slate-500">{{ item.modified }}</td>
                <td class="px-6 py-3.5 text-right">
                  <button (click)="deleteItem(item)" class="text-rose-400 hover:text-rose-300 text-xs">Удалить</button>
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
export class FilesComponent implements OnInit {
  currentPath = signal<string>('');
  items = signal<FileItem[]>([]);
  editingFile = signal<FileItem | null>(null);
  fileContent: string = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadDirectory('');
  }

  loadDirectory(path: string) {
    this.http.get<{ currentPath: string; items: FileItem[] }>(`/api/files/list?path=${encodeURIComponent(path)}`).subscribe({
      next: (res) => {
        this.currentPath.set(res.currentPath);
        this.items.set(res.items);
      },
      error: () => {}
    });
  }

  openItem(item: FileItem) {
    if (item.isDir) {
      this.loadDirectory(item.path);
    } else {
      this.http.get<{ path: string; content: string }>(`/api/files/read?path=${encodeURIComponent(item.path)}`).subscribe({
        next: (res) => {
          this.editingFile.set(item);
          this.fileContent = res.content;
        }
      });
    }
  }

  saveFile() {
    const f = this.editingFile();
    if (!f) return;
    this.http.post('/api/files/save', { path: f.path, content: this.fileContent }).subscribe({
      next: () => {
        alert('Файл сохранен!');
        this.editingFile.set(null);
      }
    });
  }

  deleteItem(item: FileItem) {
    if (confirm(`Удалить ${item.name}?`)) {
      this.http.post('/api/files/delete', { path: item.path }).subscribe({
        next: () => this.loadDirectory(this.currentPath())
      });
    }
  }

  goUp() {
    const p = this.currentPath();
    const parts = p.split('/');
    parts.pop();
    this.loadDirectory(parts.join('/'));
  }

  openNewFileModal() {
    const name = prompt('Введите имя файла (например, test.php):');
    if (name) {
      const full = this.currentPath() ? `${this.currentPath()}/${name}` : name;
      this.http.post('/api/files/create', { path: full, type: 'file' }).subscribe({
        next: () => this.loadDirectory(this.currentPath())
      });
    }
  }

  openNewFolderModal() {
    const name = prompt('Введите имя новой папки:');
    if (name) {
      const full = this.currentPath() ? `${this.currentPath()}/${name}` : name;
      this.http.post('/api/files/create', { path: full, type: 'dir' }).subscribe({
        next: () => this.loadDirectory(this.currentPath())
      });
    }
  }
}
