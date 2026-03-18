import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DeckService } from '../../services/deck.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-[70vh] flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-8 max-w-md w-full text-center">
        @if (status() === 'loading') {
          <div class="flex flex-col items-center justify-center gap-4">
            <span class="material-symbols-outlined text-[48px] text-[#4255FF] animate-spin">progress_activity</span>
            <h2 class="text-xl font-black text-gray-900 m-0">Đang xử lý lời mời</h2>
            <p class="text-gray-500 text-[14px]">Vui lòng đợi trong giây lát...</p>
          </div>
        } @else if (status() === 'success') {
          <div class="flex flex-col items-center justify-center gap-4">
            <div class="w-16 h-16 rounded-full bg-green-50 text-green-500 flex items-center justify-center mb-2">
              <span class="material-symbols-outlined text-[36px]">check_circle</span>
            </div>
            <h2 class="text-xl font-black text-gray-900 m-0">Chấp nhận thành công!</h2>
            <p class="text-gray-500 text-[14px]">Bạn đã được thêm vào bộ thẻ. Bây giờ bạn có thể xem và học bộ thẻ này.</p>
            <button routerLink="/library" class="mt-4 w-full py-3.5 bg-[#4255FF] text-white font-bold text-[14px] rounded-xl cursor-pointer hover:bg-[#3345e0] transition-colors border-none">
              Vào thư viện
            </button>
          </div>
        } @else {
          <div class="flex flex-col items-center justify-center gap-4">
            <div class="w-16 h-16 rounded-full bg-red-50 text-red-500 flex items-center justify-center mb-2">
              <span class="material-symbols-outlined text-[36px]">error</span>
            </div>
            <h2 class="text-xl font-black text-gray-900 m-0">Không thể chấp nhận lời mời</h2>
            <p class="text-red-500 font-medium text-[14px] bg-red-50 px-4 py-3 rounded-lg w-full">{{ errorMessage() }}</p>
            <p class="text-gray-500 text-[13px] mt-2">Đường dẫn có thể đã hết hạn hoặc không hợp lệ.</p>
            <button routerLink="/dashboard" class="mt-4 w-full py-3.5 bg-gray-100 text-gray-700 font-bold text-[14px] rounded-xl cursor-pointer hover:bg-gray-200 transition-colors border-none">
              Về trang chủ
            </button>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      background-color: #f9fafb;
      min-height: calc(100vh - 80px);
    }
  `]
})
export class AcceptInvite implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private deckService = inject(DeckService);

  status = signal<'loading' | 'success' | 'error'>('loading');
  errorMessage = signal<string>('');

  ngOnInit() {
    this.route.queryParamMap.subscribe(params => {
      const token = params.get('token');
      if (!token) {
        this.status.set('error');
        this.errorMessage.set('Không tìm thấy mã mời hợp lệ chặn trong đường dẫn.');
        return;
      }

      this.deckService.acceptInvite(token).subscribe({
        next: () => {
          this.status.set('success');
        },
        error: (err) => {
          this.status.set('error');
          this.errorMessage.set(err?.error?.message || 'Đã xảy ra lỗi khi xác nhận lời mời.');
        }
      });
    });
  }
}
