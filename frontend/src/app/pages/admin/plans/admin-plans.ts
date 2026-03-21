import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, AdminSubscriptionPlan, UpdatePlanRequest } from '../../../services/admin.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-admin-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-plans.html',
})
export class AdminPlans implements OnInit {
  private adminService = inject(AdminService);

  plans = signal<AdminSubscriptionPlan[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  saving = signal(false);
  successMsg = signal<string | null>(null);

  // Edit modal
  editingPlan = signal<AdminSubscriptionPlan | null>(null);
  editForm = signal<UpdatePlanRequest>({
    name: '', description: '', dailyGenerateLimit: 0, numDeckLimit: 0, price: 0, duration: 30, isActive: true, order: 1
  });
  showCreateModal = signal(false);

  ngOnInit(): void { this.loadPlans(); }

  loadPlans() {
    this.loading.set(true);
    this.adminService.getSubscriptionPlans().subscribe({
      next: (res) => {
        this.plans.set((res.data ?? []).sort((a, b) => a.order - b.order));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Không thể tải gói dịch vụ');
        this.loading.set(false);
      }
    });
  }

  openEdit(plan: AdminSubscriptionPlan) {
    this.editingPlan.set(plan);
    this.editForm.set({
      name: plan.name,
      description: plan.description,
      dailyGenerateLimit: plan.dailyGenerateLimit,
      numDeckLimit: plan.numDeckLimit,
      price: plan.price,
      duration: plan.duration,
      isActive: plan.isActive,
      order: plan.order
    });
    this.successMsg.set(null);
  }

  closeModal() {
    this.editingPlan.set(null);
    this.showCreateModal.set(false);
  }

  getDurationText(days: number) {
    if (days === 0) return 'Vĩnh viễn';
    if (days === 30) return 'Tháng';
    if (days === 365) return 'Năm';
    return `${days} ngày`;
  }

  toggleActive(plan: AdminSubscriptionPlan) {
    const newStatus = !plan.isActive;
    
    Swal.fire({
      title: 'Đổi trạng thái',
      text: `Bạn có chắc chắn muốn ${newStatus ? 'kích hoạt' : 'tạm dừng'} gói ${plan.name}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#4f46e5',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Đồng ý',
      cancelButtonText: 'Hủy bỏ'
    }).then((result) => {
      if (result.isConfirmed) {
        const updateParams: UpdatePlanRequest = { ...plan, isActive: newStatus };
        this.adminService.updateSubscriptionPlan(plan.id, updateParams).subscribe({
          next: () => {
            Swal.fire('Thành công', `Gói ${plan.name} đã được ${newStatus ? 'kích hoạt' : 'tạm dừng'}.`, 'success');
            this.loadPlans();
          },
          error: (err: any) => Swal.fire('Lỗi', 'Có lỗi xảy ra: ' + err.message, 'error')
        });
      }
    });
  }

  deletePlan(plan: AdminSubscriptionPlan) {
    Swal.fire({
      title: 'Xóa vĩnh viễn?',
      text: `Bạn có chắc chắn muốn xóa gói ${plan.name} vĩnh viễn không? Hành động này không thể hoàn tác.`,
      icon: 'error',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Xóa ngay',
      cancelButtonText: 'Hủy bỏ'
    }).then((result) => {
      if (result.isConfirmed) {
        this.adminService.deleteSubscriptionPlan(plan.id).subscribe({
          next: () => {
            this.plans.update(plans => plans.filter(p => p.id !== plan.id));
            Swal.fire('Thành công', `Đã xóa gói ${plan.name} thành công!`, 'success');
          },
          error: (err: any) => Swal.fire('Lỗi', 'Có lỗi xảy ra khi xóa: ' + err.message, 'error')
        });
      }
    });
  }

  savePlan() {
    const plan = this.editingPlan();
    if (!plan) return;
    this.saving.set(true);
    this.adminService.updateSubscriptionPlan(plan.id, this.editForm()).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMsg.set('Cập nhật thành công!');
        this.closeModal();
        this.loadPlans();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Lỗi khi cập nhật');
      }
    });
  }

  createPlan() {
    this.saving.set(true);
    this.adminService.createSubscriptionPlan(this.editForm()).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeModal();
        this.loadPlans();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Lỗi khi tạo gói');
      }
    });
  }

  openCreate() {
    this.editingPlan.set(null);
    this.editForm.set({ name: '', description: '', dailyGenerateLimit: 0, numDeckLimit: 0, price: 0, duration: 30, isActive: true, order: 1 });
    this.showCreateModal.set(true);
  }
}
