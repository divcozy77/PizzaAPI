import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PizzaService {
  private baseUrl = 'https://localhost:7161/api';

  constructor(private http: HttpClient) {}

  getPizzaCsv(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export/pizzas/csv`, { responseType: 'blob' });
  }

  getPizzas(type?: string, size?: string): Observable<any[]> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    if (size) params = params.set('size', size);
    return this.http.get<any[]>(`${this.baseUrl}/pizzas`, { params });
  }

  getOrders(from?: string, to?: string): Observable<any[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<any[]>(`${this.baseUrl}/orders`, { params });
  }
}
