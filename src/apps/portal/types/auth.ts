export interface TokenSet {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  scope: string;
  issuedAt: number; // timestamp UTC
}

export interface UserInfo {
  sub: string;
  email: string;
  name?: string;
  givenName?: string;
  familyName?: string;
  initials?: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
}