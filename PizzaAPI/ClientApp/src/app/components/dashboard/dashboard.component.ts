import { Component, Injectable, OnInit } from '@angular/core';
import { PizzaService } from '../../services/pizza.service';
import { saveAs } from 'file-saver';
import { ChartConfiguration } from 'chart.js';
import { PageEvent } from '@angular/material/paginator';

@Injectable()
@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  pizzas: any[] = [];
  orders: any[] = [];
  filters = { type: '', size: '', from: '', to: '' };

  constructor(private pizzaService: PizzaService) {}
  
  salesData: { [type: string]: number } = {};
  chartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [{ data: [], label: 'Sales by Type' }] };

  pageSize = 5;
  currentPage = 0;

  get paginatedPizzas() {
    const start = this.currentPage * this.pageSize;
    return this.pizzas.slice(start, start + this.pageSize);
  }

  onPageChange(event: PageEvent) {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
  }

  downloadCsv() {
    this.pizzaService.getPizzaCsv().subscribe(blob => saveAs(blob, 'pizzas.csv'));
  }

  updateChart() {
    const countMap: { [key: string]: number } = {};
    this.pizzas.forEach(p => {
      countMap[p.pizzaTypeId] = (countMap[p.pizzaTypeId] || 0) + 1;
    });
    this.chartData.labels = Object.keys(countMap);
    this.chartData.datasets[0].data = Object.values(countMap);
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    this.pizzaService.getPizzas(this.filters.type, this.filters.size).subscribe(p => {
      this.pizzas = p;
      this.updateChart();
    });
    this.pizzaService.getOrders(this.filters.from, this.filters.to).subscribe(o => this.orders = o);
  }
}