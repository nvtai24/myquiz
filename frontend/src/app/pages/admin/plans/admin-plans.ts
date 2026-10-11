import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AdminSubscriptionPlanResponse, UpdatePlanRequest } from '../../../models/admin.models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-admin-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-plans.html',
})
export class AdminPlans implements OnInit {
  private adminService = inject(AdminService);

  plans = signal<AdminSubscriptionPlanResponse[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  saving = signal(false);
  formError = signal<string | null>(null);

  // Edit modal
  editingPlan = signal<AdminSubscriptionPlanResponse | null>(null);
  editForm = signal<UpdatePlanRequest>({
    name: '', description: '', dailyGenerateLimit: 0, maxQuestionsPerGenerate: 20, hasExportToPdf: false, price: 0, duration: 30, isActive: true, order: 1
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
        this.error.set(err?.error?.message ?? 'Could not load subscription plans');
        this.loading.set(false);
      }
    });
  }

  openEdit(plan: AdminSubscriptionPlanResponse) {
    this.editingPlan.set(plan);
    this.editForm.set({
      name: plan.name,
      description: plan.description,
      dailyGenerateLimit: plan.dailyGenerateLimit,
      maxQuestionsPerGenerate: plan.maxQuestionsPerGenerate,
      hasExportToPdf: plan.hasExportToPdf,
      price: plan.price,
      duration: plan.duration,
      isActive: plan.isActive,
      order: plan.order
    });
    this.formError.set(null);
  }

  closeModal() {
    this.editingPlan.set(null);
    this.showCreateModal.set(false);
    this.formError.set(null);
  }

  getDurationText(days: number) {
    if (days === 0) return 'Lifetime';
    if (days === 30) return 'Month';
    if (days === 365) return 'Year';
    return `${days} days`;
  }

  toggleActive(plan: AdminSubscriptionPlanResponse) {
    const newStatus = !plan.isActive;

    Swal.fire({
      title: 'Update Status',
      text: `Are you sure you want to ${newStatus ? 'activate' : 'deactivate'} plan ${plan.name}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#4f46e5',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Confirm',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        const updateParams: UpdatePlanRequest = { ...plan, isActive: newStatus };
        this.adminService.updateSubscriptionPlan(plan.id, updateParams).subscribe({
          next: () => {
            Swal.fire('Success', `Plan ${plan.name} has been ${newStatus ? 'activated' : 'deactivated'}.`, 'success');
            this.loadPlans();
          },
          error: (err: any) => Swal.fire('Error', this.extractErrorMessage(err), 'error')
        });
      }
    });
  }

  deletePlan(plan: AdminSubscriptionPlanResponse) {
    Swal.fire({
      title: 'Delete Permanently?',
      text: `Are you sure you want to delete plan ${plan.name} permanently? This action cannot be undone.`,
      icon: 'error',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Delete Now',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.adminService.deleteSubscriptionPlan(plan.id).subscribe({
          next: () => {
            this.plans.update(plans => plans.filter(p => p.id !== plan.id));
            Swal.fire('Success', `Plan ${plan.name} deleted successfully!`, 'success');
          },
          error: (err: any) => Swal.fire('Error', this.extractErrorMessage(err), 'error')
        });
      }
    });
  }

  savePlan() {
    const plan = this.editingPlan();
    if (!plan) return;

    this.saving.set(true);
    this.formError.set(null);
    this.adminService.updateSubscriptionPlan(plan.id, this.editForm()).subscribe({
      next: () => {
        this.saving.set(false);
        Swal.fire('Success', `Plan ${plan.name} updated successfully!`, 'success');
        this.closeModal();
        this.loadPlans();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(this.extractErrorMessage(err));
      }
    });
  }

  createPlan() {
    this.saving.set(true);
    this.formError.set(null);
    this.adminService.createSubscriptionPlan(this.editForm()).subscribe({
      next: () => {
        this.saving.set(false);
        Swal.fire('Success', 'Plan created successfully!', 'success');
        this.closeModal();
        this.loadPlans();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(this.extractErrorMessage(err));
      }
    });
  }

  openCreate() {
    this.editingPlan.set(null);
    this.editForm.set({ name: '', description: '', dailyGenerateLimit: 0, maxQuestionsPerGenerate: 20, hasExportToPdf: false, price: 0, duration: 30, isActive: true, order: 1 });
    this.formError.set(null);
    this.showCreateModal.set(true);
  }

  private extractErrorMessage(err: any): string {
    return err?.error?.message ?? err?.message ?? 'An error occurred. Please try again.';
  }
}
