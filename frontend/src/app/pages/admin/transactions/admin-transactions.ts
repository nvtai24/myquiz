import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AdminPaymentResponse } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-transactions.html',
})
export class AdminTransactions implements OnInit {
  private adminService = inject(AdminService);

  searchQuery = signal('');
  filterStatus = signal<'All' | 'Pending' | 'Completed' | 'Failed' | 'Expired'>('All');
  fromDate = signal('');
  toDate = signal('');
  currentPage = signal(1);
  pageSize = signal(10);
  
  payments = signal<AdminPaymentResponse[]>([]);
  totalPayments = signal(0);
  totalPages = signal(0);

  pagesList = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    if (total <= 1) return [1];

    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);
    
    if (end - start < 4) {
      if (start === 1) {
        end = Math.min(total, 5);
      } else if (end === total) {
        start = Math.max(1, total - 4);
      }
    }

    const pages = [];
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  });

  ngOnInit() {
    this.loadPayments();
  }

  loadPayments() {
    let statusParam = this.filterStatus() === 'All' ? undefined : this.filterStatus();
    
    // Convert "YYYY-MM-DD" to ISO-8601 date-time format for backend
    let fromParam = this.fromDate() ? `${this.fromDate()}T00:00:00Z` : undefined;
    let toParam = this.toDate() ? `${this.toDate()}T23:59:59Z` : undefined;

    this.adminService.getPayments(this.currentPage(), this.pageSize(), this.searchQuery(), statusParam, fromParam, toParam).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.payments.set(res.data);
          this.totalPayments.set(res.pagination?.totalRecords || res.data.length);
          this.totalPages.set(res.pagination?.totalPages || 1);
        }
      },
      error: (err) => console.error('Error fetching payments:', err)
    });
  }

  onFilterChange() {
    this.currentPage.set(1);
    this.loadPayments();
  }

  onSearch() {
    this.currentPage.set(1);
    this.loadPayments();
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadPayments();
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadPayments();
    }
  }

  goToPage(p: number) {
    if (p >= 1 && p <= this.totalPages() && p !== this.currentPage()) {
      this.currentPage.set(p);
      this.loadPayments();
    }
  }

  selectedPayment = signal<AdminPaymentResponse | null>(null);

  openPaymentDetails(payment: AdminPaymentResponse) {
    this.selectedPayment.set(payment);
  }

  closePaymentDetails() {
    this.selectedPayment.set(null);
  }

  getStatusColor(status: string | undefined) {
    if (!status) return 'bg-gray-50 text-gray-700 border-gray-200';
    if (status.includes('Completed') || status.includes('Success')) return 'bg-green-50 text-green-700 border-green-200';
    if (status.includes('Expired') || status.includes('Failed')) return 'bg-red-50 text-red-700 border-red-200';
    if (status.includes('Pending')) return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    return 'bg-blue-50 text-blue-700 border-blue-200';
  }
}
