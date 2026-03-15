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
    name: '', price: 0, billingCycle: 'monthly', features: [], isActive: true
  });
  newFeature = signal('');
  showCreateModal = signal(false);

  ngOnInit(): void { this.loadPlans(); }

  loadPlans() {
    this.loading.set(true);
    this.adminService.getSubscriptionPlans().subscribe({
      next: (res) => {
        this.plans.set(res.data ?? []);
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
      price: plan.price,
      billingCycle: plan.billingCycle,
      features: [...(plan.features ?? [])],
      isActive: plan.isActive,
    });
    this.successMsg.set(null);
  }

  closeModal() {
    this.editingPlan.set(null);
    this.showCreateModal.set(false);
    this.newFeature.set('');
  }

  addFeature() {
    const f = this.newFeature().trim();
    if (!f) return;
    this.editForm.update(form => ({ ...form, features: [...form.features, f] }));
    this.newFeature.set('');
  }

  removeFeature(i: number) {
    this.editForm.update(form => ({
      ...form,
      features: form.features.filter((_, idx) => idx !== i)
    }));
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
    this.editForm.set({ name: '', price: 0, billingCycle: 'monthly', features: [], isActive: true });
    this.showCreateModal.set(true);
  }
}
