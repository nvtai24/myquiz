// ── Response DTOs ──

export interface ProfileResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl: string;
  roles: string[];
  currentPlan: string;
}

// Note: Profile update uses FormData with fields:
// - FirstName: string
// - LastName: string
// - avatar: File (optional)
