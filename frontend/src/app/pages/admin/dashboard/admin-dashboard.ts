import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService, AdminPayment } from '../../../services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html'
})
export class AdminDashboard implements OnInit {
  private adminService = inject(AdminService);

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

  // REAL: Recent Transactions (Sử dụng tái lập API vừa code)
  recentPayments = signal<AdminPayment[]>([]);
  loadingPayments = signal(true);

  ngOnInit() {
    this.fetchRecentPayments();
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

  // Utils for UI
  getBarHeight(amount: number) {
    const max = Math.max(...this.chartData().map(d => d.amount));
    if (max === 0) return '0%';
    return `${(amount / max) * 100}%`;
  }

  planTotalUsers() {
    return this.planDist().reduce((sum, p) => sum + p.userCount, 0);
  }

  getPieChartStyle() {
    const plans = this.planDist();
    let gradients: string[] = [];
    let currentPercent = 0;
    
    for (const plan of plans) {
      const nextPercent = currentPercent + plan.width;
      gradients.push(`${plan.hex} ${currentPercent}% ${nextPercent}%`);
      currentPercent = nextPercent;
    }
    return `conic-gradient(${gradients.join(', ')})`;
  }

  getStatusColor(status: string | undefined) {
    if (!status) return 'bg-gray-50 text-gray-700 border-gray-200';
    if (status.includes('Completed') || status.includes('Success')) return 'bg-green-50 text-green-700 border-green-200';
    if (status.includes('Expired') || status.includes('Failed')) return 'bg-red-50 text-red-700 border-red-200';
    if (status.includes('Pending')) return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    return 'bg-blue-50 text-blue-700 border-blue-200';
  }
}
