import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, AdminSubscriptionPlan, UpdatePlanRequest } from '../../../services/admin.service';

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
    if (confirm(`Bạn có chắc chắn muốn ${newStatus ? 'kích hoạt' : 'tạm dừng'} gói ${plan.name}?`)) {
      const updateReq: UpdatePlanRequest = {
        name: plan.name,
        description: plan.description,
        dailyGenerateLimit: plan.dailyGenerateLimit,
        numDeckLimit: plan.numDeckLimit,
        price: plan.price,
        duration: plan.duration,
        isActive: newStatus,
        order: plan.order
      };
      
      this.adminService.updateSubscriptionPlan(plan.id, updateReq).subscribe({
        next: () => {
          this.plans.update(plans => plans.map(p => 
            p.id === plan.id ? { ...p, isActive: newStatus } : p
          ));
        },
        error: (err) => alert('Có lỗi xảy ra: ' + err.message)
      });
    }
  }

  deletePlan(plan: AdminSubscriptionPlan) {
    if (confirm(`Bạn có chắc chắn muốn xóa gói ${plan.name} vĩnh viễn không? Hành động này không thể hoàn tác.`)) {
      this.adminService.deleteSubscriptionPlan(plan.id).subscribe({
        next: () => {
          this.plans.update(plans => plans.filter(p => p.id !== plan.id));
          this.successMsg.set('Đã xoá gói thành công!');
        },
        error: (err) => alert('Có lỗi xảy ra khi xóa: ' + err.message)
      });
    }
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
