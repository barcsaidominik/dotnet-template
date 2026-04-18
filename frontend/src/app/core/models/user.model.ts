export interface User {
  id: string;
  email: string;
  facilityId: string | null;
  isApproved: boolean;
  role: string | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  role: string;
  refreshToken: string;
}
