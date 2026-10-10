import { clearSession, getRefreshToken, refreshAccessToken, request, setSession } from './client'
import type { Me, RegisterRequest, TokenResponse } from './types'

export async function login(email: string, password: string): Promise<void> {
  const tokens = await request<TokenResponse>('/api/auth/login', {
    method: 'POST',
    body: { email, password },
    auth: false,
  })
  setSession(tokens)
}

export async function register(body: RegisterRequest): Promise<void> {
  await request<unknown>('/api/auth/register', { method: 'POST', body, auth: false })
}

export async function verifyEmail(token: string): Promise<void> {
  await request<boolean>('/api/auth/verify-email', { method: 'POST', body: { token }, auth: false })
}

export function fetchMe(): Promise<Me> {
  return request<Me>('/api/auth/me')
}

// Sayfa yenilenince: refresh token varsa sessizce yeni access token al.
export function restoreSession(): Promise<boolean> {
  return refreshAccessToken()
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()
  try {
    if (refreshToken) {
      await request<boolean>('/api/auth/logout', { method: 'POST', body: { refreshToken }, auth: false })
    }
  } catch {
    // Sunucuya ulaşılamasa da yerel oturum kapatılır.
  } finally {
    clearSession()
  }
}
