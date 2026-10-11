import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AdminUserResponse } from '../../../models/admin.models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html',
})
export class AdminUsers implements OnInit {
  private adminService = inject(AdminService);

  searchQuery = signal('');
  filterRole = signal<'All' | 'Admin' | 'User'>('All');
  filterStatus = signal<'All' | 'Active' | 'Banned'>('All');
  currentPage = signal(1);
  pageSize = signal(10);
  
  users = signal<AdminUserResponse[]>([]);
  totalUsers = signal(0);
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
    this.loadUsers();
  }

  loadUsers() {
    let roleParam = this.filterRole() === 'All' ? undefined : this.filterRole();
    let isBannedParam: boolean | undefined = undefined;
    if (this.filterStatus() === 'Active') isBannedParam = false;
    else if (this.filterStatus() === 'Banned') isBannedParam = true;

    this.adminService.getUsers(this.currentPage(), this.pageSize(), this.searchQuery(), roleParam, isBannedParam).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.users.set(res.data);
          this.totalUsers.set(res.pagination?.totalRecords || res.data.length);
          this.totalPages.set(res.pagination?.totalPages || 1);
        }
      },
      error: (err) => console.error('Error fetching users:', err)
    });
  }

  onFilterChange() {
    this.currentPage.set(1);
    this.loadUsers();
  }

  onSearch() {
    this.currentPage.set(1);
    this.loadUsers();
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadUsers();
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadUsers();
    }
  }

  goToPage(p: number) {
    if (p >= 1 && p <= this.totalPages() && p !== this.currentPage()) {
      this.currentPage.set(p);
      this.loadUsers();
    }
  }

  toggleBan(user: AdminUserResponse) {
    const newStatus = !user.isBanned;
    
    Swal.fire({
      title: 'Confirm Action',
      text: `Are you sure you want to ${newStatus ? 'ban' : 'unban'} user ${user.email}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#4f46e5',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Yes, proceed',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.adminService.updateUserBanStatus(user.id, newStatus).subscribe({
          next: () => {
            Swal.fire('Success', `User has been ${newStatus ? 'banned' : 'unbanned'} successfully!`, 'success');
            this.loadUsers();
          },
          error: (err: any) => Swal.fire('Error', 'An error occurred: ' + err.message, 'error')
        });
      }
    });
  }

  changeRole(user: AdminUserResponse, newRole: string) {
    if (newRole === this.getRoleName(user.roles)) return;
    
    Swal.fire({
      title: 'Change Permissions',
      text: `Are you sure you want to assign ${newRole} role to account ${user.email}?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#4f46e5',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Update Role',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.adminService.assignUserRole(user.id, newRole).subscribe({
          next: () => {
            Swal.fire('Success', `User role updated to ${newRole} successfully!`, 'success');
            this.loadUsers();
          },
          error: (err: any) => {
            Swal.fire('Error', 'An error occurred: ' + err.message, 'error');
            this.loadUsers(); // revert
          }
        });
      } else {
        this.loadUsers(); // revert selection if cancelled
      }
    });
  }

  getRoleColor(roles: string[] | undefined) {
    if (!roles || roles.length === 0) return 'bg-gray-50 text-gray-700 border-gray-200';
    return roles.includes('Admin') ? 'bg-indigo-50 text-indigo-700 border-indigo-200' : 'bg-blue-50 text-blue-700 border-blue-200';
  }

  getRoleName(roles: string[] | undefined) {
    if (!roles || roles.length === 0) return 'User';
    return roles.includes('Admin') ? 'Admin' : 'User';
  }

  AVATAR_COLORS = ['#4255FF', '#7c3aed', '#059669', '#dc2626', '#d97706', '#0891b2', '#be185d'];
  getAvatarColor(id: string) {
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
        hash = id.charCodeAt(i) + ((hash << 5) - hash);
    }
    return this.AVATAR_COLORS[Math.abs(hash) % this.AVATAR_COLORS.length];
  }

  getAvatarInitial(user: AdminUserResponse) {
    return user.firstName ? user.firstName.charAt(0).toUpperCase() : (user.email ? user.email.charAt(0).toUpperCase() : '?');
  }
}
