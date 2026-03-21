import { Component, OnInit, inject, signal, computed, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService, AdminPayment } from '../../../services/admin.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html'
})
export class AdminDashboard implements OnInit, AfterViewInit, OnDestroy {
  private adminService = inject(AdminService);

  @ViewChild('revenueChartCanvas') revenueChartCanvas!: ElementRef;
  @ViewChild('planChartCanvas') planChartCanvas!: ElementRef;

  revenueChartInstance: Chart | null = null;
  planChartInstance: Chart | null = null;

  // MOCK: Overview Stats
  totalRevenue = signal(15400000);
  monthlyRevenue = signal(3200000);
  revenueGrowth = signal(12.5);
  newUsers = signal(145);
  userGrowth = signal(8.2);

  // MOCK: Revenue Chart
  chartPeriod = signal<'7days' | '30days'>('7days');

  chartData7Days = signal([
    { date: '15/03', amount: 300000 },
    { date: '16/03', amount: 500000 },
    { date: '17/03', amount: 200000 },
    { date: '18/03', amount: 800000 },
    { date: '19/03', amount: 0 },
    { date: '20/03', amount: 450000 },
    { date: '21/03', amount: 950000 },
  ]);

  chartData30Days = signal(
    Array.from({ length: 30 }).map((_, i) => {
      const d = new Date(2026, 2, 21 - 29 + i);
      return {
        date: `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}`,
        amount: Math.floor(Math.random() * 1200000)
      };
    })
  );

  chartData = computed(() => this.chartPeriod() === '7days' ? this.chartData7Days() : this.chartData30Days());

  // MOCK: Plan Distribution
  planDist = signal([
    { planName: 'Free', userCount: 450, color: '#9ca3af', bg: 'bg-gray-200', width: 60, hex: '#e5e7eb' },
    { planName: 'Pro', userCount: 220, color: '#4f46e5', bg: 'bg-indigo-500', width: 30, hex: '#6366f1' },
    { planName: 'Premium', userCount: 80, color: '#f59e0b', bg: 'bg-amber-500', width: 10, hex: '#f59e0b' },
  ]);

  planTotalUsers() {
    return this.planDist().reduce((sum, p) => sum + p.userCount, 0);
  }

  // REAL: Recent Transactions
  recentPayments = signal<AdminPayment[]>([]);
  loadingPayments = signal(true);

  ngOnInit() {
    this.fetchRecentPayments();
  }

  ngAfterViewInit() {
    this.initRevenueChart();
    this.initPlanChart();
  }

  ngOnDestroy() {
    if (this.revenueChartInstance) this.revenueChartInstance.destroy();
    if (this.planChartInstance) this.planChartInstance.destroy();
  }

  fetchRecentPayments() {
    this.adminService.getPayments(1, 4, '', 'All').subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.recentPayments.set(res.data);
        }
        this.loadingPayments.set(false);
      },
      error: () => this.loadingPayments.set(false)
    });
  }

  switchPeriod(period: '7days' | '30days') {
    this.chartPeriod.set(period);
    this.updateRevenueChart();
  }

  initRevenueChart() {
    if (!this.revenueChartCanvas) return;
    const ctx = this.revenueChartCanvas.nativeElement.getContext('2d');
    
    this.revenueChartInstance = new Chart(ctx, {
      type: 'bar',
      data: this.getRevenueChartData(),
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#1f2937',
            titleFont: { size: 13, family: 'Inter' },
            bodyFont: { size: 14, weight: 'bold', family: 'Inter' },
            padding: 12,
            cornerRadius: 8,
            displayColors: false,
            callbacks: {
              label: (context) => context.raw?.toLocaleString() + ' VNĐ'
            }
          }
        },
        scales: {
          y: { 
            beginAtZero: true, 
            grid: { color: '#f3f4f6' },
            border: { display: false },
            ticks: { color: '#9ca3af', font: { size: 11 }, callback: (val) => (Number(val) / 1000) + 'k' }
          },
          x: { 
            grid: { display: false },
            border: { display: false },
            ticks: { 
              color: '#9ca3af', 
              font: { size: 11 },
              maxTicksLimit: this.chartPeriod() === '7days' ? 7 : 10
            }
          }
        }
      }
    });
  }

  updateRevenueChart() {
    if (this.revenueChartInstance) {
      this.revenueChartInstance.data = this.getRevenueChartData();
      if (this.revenueChartInstance.options.scales?.['x']?.ticks) {
         this.revenueChartInstance.options.scales['x'].ticks.maxTicksLimit = this.chartPeriod() === '7days' ? 7 : 10;
      }
      this.revenueChartInstance.update();
    }
  }

  getRevenueChartData() {
    const data = this.chartData();
    return {
      labels: data.map(d => d.date),
      datasets: [{
        label: 'Doanh thu',
        data: data.map(d => d.amount),
        backgroundColor: '#6366f1',
        hoverBackgroundColor: '#4f46e5',
        borderRadius: 4,
        barPercentage: this.chartPeriod() === '7days' ? 0.5 : 0.8
      }]
    };
  }

  initPlanChart() {
    if (!this.planChartCanvas) return;
    const ctx = this.planChartCanvas.nativeElement.getContext('2d');
    const data = this.planDist();

    this.planChartInstance = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: data.map(d => d.planName),
        datasets: [{
          data: data.map(d => d.userCount),
          backgroundColor: data.map(d => d.hex),
          borderWidth: 0,
          hoverOffset: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '75%',
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#1f2937',
            bodyFont: { size: 13, weight: 'bold', family: 'Inter' },
            padding: 10,
            cornerRadius: 8,
            displayColors: true,
            callbacks: {
              label: (context) => ` ${context.label}: ${context.raw} users`
            }
          }
        }
      }
    });
  }

  getStatusColor(status: string | undefined) {
    if (!status) return 'bg-gray-50 text-gray-700 border-gray-200';
    if (status.includes('Completed') || status.includes('Success')) return 'bg-green-50 text-green-700 border-green-200';
    if (status.includes('Expired') || status.includes('Failed')) return 'bg-red-50 text-red-700 border-red-200';
    if (status.includes('Pending')) return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    return 'bg-blue-50 text-blue-700 border-blue-200';
  }
}
