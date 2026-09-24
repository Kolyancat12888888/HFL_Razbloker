import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'sites',
    loadComponent: () => import('./pages/sites/sites.component').then(m => m.SitesComponent)
  },
  {
    path: 'databases',
    loadComponent: () => import('./pages/databases/databases.component').then(m => m.DatabasesComponent)
  },
  {
    path: 'dns',
    loadComponent: () => import('./pages/dns/dns.component').then(m => m.DnsComponent)
  },
  {
    path: 'ssl',
    loadComponent: () => import('./pages/ssl/ssl.component').then(m => m.SslComponent)
  },
  {
    path: 'files',
    loadComponent: () => import('./pages/files/files.component').then(m => m.FilesComponent)
  },
  {
    path: 'terminal',
    loadComponent: () => import('./pages/terminal/terminal.component').then(m => m.TerminalComponent)
  },
  {
    path: 'cron',
    loadComponent: () => import('./pages/cron/cron.component').then(m => m.CronComponent)
  },
  {
    path: 'mail',
    loadComponent: () => import('./pages/mail/mail.component').then(m => m.MailComponent)
  },
  {
    path: 'security',
    loadComponent: () => import('./pages/security/security.component').then(m => m.SecurityComponent)
  },
  {
    path: 'backups',
    loadComponent: () => import('./pages/backups/backups.component').then(m => m.BackupsComponent)
  },
  {
    path: 'users',
    loadComponent: () => import('./pages/users/users.component').then(m => m.UsersComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
