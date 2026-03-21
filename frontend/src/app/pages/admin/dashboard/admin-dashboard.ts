import { Component, OnInit, inject, signal, computed, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService, AdminPayment, RevenueData, PlanDistData } from '../../../services/admin.service';
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

  // Overview Stats
  totalRevenue = signal(0);
  monthlyRevenue = signal(0);
  revenueGrowth = signal(0);
  newUsers = signal(0);
  userGrowth = signal(0);

  // Revenue Chart
  chartPeriod = signal<'7days' | '30days'>('7days');
  chartData = signal<RevenueData[]>([]);

  // Plan Distribution
  planDistApiData = signal<PlanDistData[]>([]);
  
  planDist = computed(() => {
    const apiData = this.planDistApiData();
    const total = apiData.reduce((sum, item) => sum + item.userCount, 0);
    const colors = ['#6366f1', '#e5e7eb', '#f59e0b', '#10b981', '#3b82f6'];
    
    return apiData.map((item, index) => {
      const percentage = total > 0 ? (item.userCount / total) * 100 : 0;
      return {
        planName: item.planName,
        userCount: item.userCount,
        width: Math.round(percentage),
        hex: colors[index % colors.length]
      };
    });
  });

  planTotalUsers() {
    return this.planDist().reduce((sum, p) => sum + p.userCount, 0);
  }

  // Recent Transactions
  recentPayments = signal<AdminPayment[]>([]);
  loadingPayments = signal(true);

  ngOnInit() {
    this.fetchSummary();
    this.fetchRecentPayments();
  }

  ngAfterViewInit() {
    // Need to initialize API data before drawing, so we fetch after view init as well for charts
    this.fetchRevenueChart(true);
    this.fetchPlanDist();
  }

  ngOnDestroy() {
    if (this.revenueChartInstance) this.revenueChartInstance.destroy();
    if (this.planChartInstance) this.planChartInstance.destroy();
  }

  fetchSummary() {
    this.adminService.getSummaryStats().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.totalRevenue.set(res.data.totalRevenue);
          this.monthlyRevenue.set(res.data.monthlyRevenue);
          this.revenueGrowth.set(res.data.revenueGrowth);
          this.newUsers.set(res.data.newUsers);
          this.userGrowth.set(res.data.userGrowth);
        }
      }
    });
  }

  fetchRevenueChart(isInit: boolean = false) {
    const days = this.chartPeriod() === '7days' ? 7 : 30;
    this.adminService.getRevenueChart(days).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.chartData.set(res.data);
          if (isInit) {
            this.initRevenueChart();
          } else {
            this.updateRevenueChart();
          }
        }
      }
    });
  }

  fetchPlanDist() {
    this.adminService.getPlanDistribution().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.planDistApiData.set(res.data);
          this.initPlanChart();
        }
      }
    });
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
    if (this.chartPeriod() === period) return;
    this.chartPeriod.set(period);
    this.fetchRevenueChart(false);
  }

  initRevenueChart() {
    if (!this.revenueChartCanvas) return;
    const ctx = this.revenueChartCanvas.nativeElement.getContext('2d');
    if (this.revenueChartInstance) this.revenueChartInstance.destroy();
    
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
    if (this.planChartInstance) this.planChartInstance.destroy();

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
