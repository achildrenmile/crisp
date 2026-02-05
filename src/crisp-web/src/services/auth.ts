const API_BASE = '/api/auth';
const TOKEN_KEY = 'crisp_token';
const USER_KEY = 'crisp_user';

export interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  userName: string;
  roles: string[];
}

export interface UserInfo {
  id: string;
  name: string;
  roles: string[];
  authMethod: string;
}

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser(): UserInfo | null {
  const userJson = localStorage.getItem(USER_KEY);
  if (!userJson) return null;
  try {
    return JSON.parse(userJson);
  } catch {
    return null;
  }
}

export function setAuthData(token: string, user: UserInfo): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearAuthData(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function isAuthenticated(): boolean {
  return !!getStoredToken();
}

export async function login(apiKey: string): Promise<TokenResponse> {
  const response = await fetch(`${API_BASE}/token`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ apiKey }),
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Invalid API key');
    }
    throw new Error(`Login failed: ${response.statusText}`);
  }

  const data: TokenResponse = await response.json();

  // Store the token and user info
  setAuthData(data.accessToken, {
    id: '', // Will be filled by /me endpoint if needed
    name: data.userName,
    roles: data.roles,
    authMethod: 'api_key',
  });

  return data;
}

export function logout(): void {
  clearAuthData();
  window.location.href = '/login';
}

export async function validateToken(): Promise<boolean> {
  const token = getStoredToken();
  if (!token) return false;

  try {
    const response = await fetch(`${API_BASE}/validate`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function getCurrentUser(): Promise<UserInfo | null> {
  const token = getStoredToken();
  if (!token) return null;

  try {
    const response = await fetch(`${API_BASE}/me`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) return null;
    return response.json();
  } catch {
    return null;
  }
}

// Helper to get auth headers for API calls
export function getAuthHeaders(): Record<string, string> {
  const token = getStoredToken();
  if (!token) return {};
  return {
    'Authorization': `Bearer ${token}`,
  };
}
