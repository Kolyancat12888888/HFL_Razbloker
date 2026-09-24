import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, switchMap, startWith } from 'rxjs';

export interface ServerStat {
  cpuUsage: number;
  ramUsage: number;
  ramTotal: string;
  ramUsed: string;
  diskUsage: number;
  diskTotal: string;
  diskUsed: string;
  uptime: string;
  activeSites: number;
  activeDbs: number;
  dnsQueriesTotal: number;
}

export interface ServiceStatus {
  name: string;
  description: string;
  status: 'running' | 'stopped' | 'restarting';
  port: string;
}

@Injectable({
  providedIn: 'root'
})
export class SystemMonitorService {
  private readonly apiUrl = '/api/system';

  constructor(private http: HttpClient) {}

  getRealMetrics(): Observable<ServerStat> {
    return this.http.get<ServerStat>(`${this.apiUrl}/metrics`);
  }

  getLiveMetricsPolling(intervalMs: number = 2500): Observable<ServerStat> {
    return interval(intervalMs).pipe(
      startWith(0),
      switchMap(() => this.getRealMetrics())
    );
  }

  getServicesStatus(): Observable<ServiceStatus[]> {
    return this.http.get<ServiceStatus[]>(`${this.apiUrl}/services`);
  }

  restartService(name: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/services/restart`, { name });
  }
}
