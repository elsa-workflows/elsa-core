export interface LoginResponse {
  isAuthenticated: boolean;
  accessToken: string;
  refreshToken: string;
}

export interface SignedInArgs {
}
