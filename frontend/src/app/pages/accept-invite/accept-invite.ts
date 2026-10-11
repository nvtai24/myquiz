import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import Swal from 'sweetalert2';
import { DeckService } from '../../services/deck.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    @if (isProcessing()) {
      <div class="min-h-[70vh] flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-8 max-w-md w-full text-center">
          <div class="flex flex-col items-center justify-center gap-4">
            <span class="material-symbols-outlined text-[48px] text-[#4255FF] animate-spin">progress_activity</span>
            <h2 class="text-xl font-black text-gray-900 m-0">Processing Invitation</h2>
            <p class="text-gray-500 text-[14px]">
              Please wait while we verify your invitation link.
            </p>
          </div>
        </div>
      </div>
    }
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
  isProcessing = signal(true);

  ngOnInit() {
    this.route.queryParamMap.subscribe(params => {
      const token = params.get('token');

      if (!token) {
        this.showErrorAndRedirect(
          'Invalid Invitation',
          'The invitation link is missing a valid token.',
          '/dashboard',
        );
        return;
      }

      this.deckService.acceptInvite(token).subscribe({
        next: (res) => {
          if (res?.success) {
            this.showSuccessAndRedirect(
              'Invitation Accepted',
              res.message || 'You have successfully joined this deck.',
              '/library',
            );
            return;
          }

          this.showErrorAndRedirect(
            'Invitation Failed',
            res?.message || 'This invitation could not be accepted with the current account.',
            '/dashboard',
          );
        },
        error: (err) => {
          this.showErrorAndRedirect(
            'Invitation Failed',
            err?.error?.message || 'An error occurred while accepting the invitation.',
            '/dashboard',
          );
        }
      });
    });
  }

  private async showSuccessAndRedirect(title: string, text: string, redirectTo: string) {
    this.isProcessing.set(false);
    await Swal.fire({
      title,
      text,
      icon: 'success',
      confirmButtonColor: '#4255FF',
      confirmButtonText: 'Go to Library',
      allowOutsideClick: false,
    });

    this.router.navigate([redirectTo]);
  }

  private async showErrorAndRedirect(title: string, text: string, redirectTo: string) {
    this.isProcessing.set(false);
    await Swal.fire({
      title,
      text,
      icon: 'error',
      confirmButtonColor: '#4255FF',
      confirmButtonText: 'Back to Dashboard',
      allowOutsideClick: false,
    });

    this.router.navigate([redirectTo]);
  }
}
