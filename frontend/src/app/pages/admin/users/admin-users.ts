import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, AdminUser } from '../../../services/admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html',
})
export class AdminUsers implements OnInit {
  private adminService = inject(AdminService);

  searchQuery = signal('');
  currentPage = signal(1);
  pageSize = signal(10);
  
  users = signal<AdminUser[]>([]);
  totalUsers = signal(0);
  totalPages = signal(0);

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.adminService.getUsers(this.currentPage(), this.pageSize(), this.searchQuery()).subscribe({
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

  toggleBan(user: AdminUser) {
    const newStatus = !user.isBanned;
    if (confirm(`Bạn có chắc chắn muốn ${newStatus ? 'khóa' : 'mở khóa'} tài khoản ${user.email}?`)) {
      this.adminService.updateUserBanStatus(user.id, newStatus).subscribe({
        next: (res) => {
          if (res.success) {
            this.users.update(users => users.map(u => 
              u.id === user.id ? { ...u, isBanned: newStatus } : u
            ));
          }
        },
        error: (err) => alert('Có lỗi xảy ra: ' + err.message)
      });
    }
  }

  changeRole(user: AdminUser, newRole: string) {
    if (confirm(`Bạn có chắc chắn muốn phân quyền ${newRole} cho tài khoản ${user.email}?`)) {
      this.adminService.assignUserRole(user.id, newRole).subscribe({
        next: (res) => {
          if (res.success) {
            this.users.update(users => users.map(u => 
              u.id === user.id ? { ...u, roles: [newRole] } : u
            ));
          }
        },
        error: (err) => alert('Có lỗi xảy ra: ' + err.message)
      });
    } else {
      // Force UI refresh to revert the select element visual state
      this.users.update(u => [...u]);
    }
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

  getAvatarInitial(user: AdminUser) {
    return user.firstName ? user.firstName.charAt(0).toUpperCase() : (user.email ? user.email.charAt(0).toUpperCase() : '?');
  }
}
