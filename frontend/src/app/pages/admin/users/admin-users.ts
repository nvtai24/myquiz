import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface MockUser {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'User';
  status: 'Active' | 'Banned';
  joinedDate: string;
  avatar: string;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html',
})
export class AdminUsers {
  searchQuery = signal('');
  filterRole = signal<'All' | 'Admin' | 'User'>('All');
  filterStatus = signal<'All' | 'Active' | 'Banned'>('All');
  currentPage = signal(1);
  readonly PAGE_SIZE = 8;

  // NOTE: Backend không có admin user list API, dữ liệu demo
  allUsers = signal<MockUser[]>([
    { id: '1', name: 'Nguyễn Văn Anh', email: 'vanAnh@example.com', role: 'Admin', status: 'Active', joinedDate: '2024-01-15', avatar: 'N' },
    { id: '2', name: 'Trần Thị Bình', email: 'thibinh@example.com', role: 'User', status: 'Active', joinedDate: '2024-02-20', avatar: 'T' },
    { id: '3', name: 'Lê Quang Cường', email: 'quangcuong@example.com', role: 'User', status: 'Active', joinedDate: '2024-03-01', avatar: 'L' },
    { id: '4', name: 'Phạm Thị Dung', email: 'thidung@example.com', role: 'User', status: 'Banned', joinedDate: '2024-03-10', avatar: 'P' },
    { id: '5', name: 'Hoàng Minh Em', email: 'minhem@example.com', role: 'User', status: 'Active', joinedDate: '2024-04-05', avatar: 'H' },
    { id: '6', name: 'Vũ Thị Phương', email: 'thiphuong@example.com', role: 'User', status: 'Active', joinedDate: '2024-04-18', avatar: 'V' },
    { id: '7', name: 'Đặng Văn Giang', email: 'vangiang@example.com', role: 'User', status: 'Active', joinedDate: '2024-05-02', avatar: 'Đ' },
    { id: '8', name: 'Bùi Thị Hoa', email: 'thihoa@example.com', role: 'Admin', status: 'Active', joinedDate: '2024-05-15', avatar: 'B' },
    { id: '9', name: 'Cao Đình Khoa', email: 'dinhkhoa@example.com', role: 'User', status: 'Banned', joinedDate: '2024-06-01', avatar: 'C' },
    { id: '10', name: 'Đinh Thị Lan', email: 'thilan@example.com', role: 'User', status: 'Active', joinedDate: '2024-06-20', avatar: 'Đ' },
  ]);

  filteredUsers = computed(() => {
    let users = this.allUsers();
    const q = this.searchQuery().toLowerCase();
    if (q) users = users.filter(u => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
    if (this.filterRole() !== 'All') users = users.filter(u => u.role === this.filterRole());
    if (this.filterStatus() !== 'All') users = users.filter(u => u.status === this.filterStatus());
    return users;
  });

  pagedUsers = computed(() => {
    const start = (this.currentPage() - 1) * this.PAGE_SIZE;
    return this.filteredUsers().slice(start, start + this.PAGE_SIZE);
  });

  totalPages = computed(() => Math.ceil(this.filteredUsers().length / this.PAGE_SIZE));

  toggleBan(user: MockUser) {
    this.allUsers.update(list => list.map(u =>
      u.id === user.id ? { ...u, status: u.status === 'Active' ? 'Banned' : 'Active' } : u
    ));
  }

  prevPage() { if (this.currentPage() > 1) this.currentPage.update(p => p - 1); }
  nextPage() { if (this.currentPage() < this.totalPages()) this.currentPage.update(p => p + 1); }

  getRoleColor(role: string) {
    return role === 'Admin' ? 'bg-indigo-50 text-indigo-700 border-indigo-200' : 'bg-blue-50 text-blue-700 border-blue-200';
  }

  AVATAR_COLORS = ['#4255FF', '#7c3aed', '#059669', '#dc2626', '#d97706', '#0891b2', '#be185d'];
  getAvatarColor(id: string) { return this.AVATAR_COLORS[parseInt(id) % this.AVATAR_COLORS.length]; }
}
