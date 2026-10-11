import { inject, Injectable, NgZone, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';

declare const google: any;

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {
  private platformId = inject(PLATFORM_ID);
  private ngZone = inject(NgZone);
  private initialized = false;
  private callbackFn: ((idToken: string) => void) | null = null;

  /**
   * Load the Google Identity Services script dynamically (browser-only).
   */
  private loadScript(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return Promise.reject('Not in browser');
    }

    return new Promise((resolve, reject) => {
      if (typeof google !== 'undefined' && google.accounts) {
        resolve();
        return;
      }

      if (document.getElementById('gsi-script')) {
        // Script tag exists but not loaded yet — wait for it
        const existing = document.getElementById('gsi-script') as HTMLScriptElement;
        existing.addEventListener('load', () => resolve());
        existing.addEventListener('error', () => reject('Failed to load Google script'));
        return;
      }

      const script = document.createElement('script');
      script.id = 'gsi-script';
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject('Failed to load Google Identity Services script');
      document.head.appendChild(script);
    });
  }

  /**
   * Initialize Google Identity Services (once).
   */
  private initializeGsi(): void {
    if (this.initialized) return;

    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: any) => {
        this.ngZone.run(() => {
          if (response.credential && this.callbackFn) {
            this.callbackFn(response.credential);
          }
        });
      },
      auto_select: false,
    });

    this.initialized = true;
  }

  /**
   * Render Google's invisible sign-in button into a container element.
   * The container should overlay a custom-styled button for consistent UI.
   *
   * @param element  DOM element to render Google's button into
   * @param callback Called with the id_token when user signs in
   */
  renderButton(element: HTMLElement, callback: (idToken: string) => void): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.callbackFn = callback;

    this.loadScript()
      .then(() => {
        this.initializeGsi();

        google.accounts.id.renderButton(element, {
          type: 'standard',
          size: 'large',
          theme: 'outline',
          text: 'signin_with',
          width: element.offsetWidth || 400,
        });
      })
      .catch(err => console.error('Google Auth: ', err));
  }
}
