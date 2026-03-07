import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.css',
})
export class AccountSettings {
  fullName = signal('Alex Johnson');
  username = signal('@alex_j');
  email = signal('alex.johnson@example.com');
  emailVerified = signal(true);
  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  twoFactorEnabled = signal(true);
  emailNotifications = signal(false);
  avatarUrl = signal<string | null>(null);

  onAvatarSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file && file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = () => this.avatarUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    }
  }

  removeAvatar() {
    this.avatarUrl.set(null);
  }

  saveChanges() {
    // TODO: call API
  }

  discard() {
    // TODO: reset form
  }

  deleteAccount() {
    // TODO: confirm & delete
  }
}
