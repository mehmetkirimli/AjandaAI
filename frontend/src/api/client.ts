// Tek API istemcisi (ADR 0021 Karar 2). Bileşenler fetch çağırmaz; hepsi request() üzerinden geçer.
// - Access token yalnızca bellekte; refresh token localStorage'da.
// - 401'de BİR refresh denenir; eşzamanlı 401'ler aynı refresh promise'ini bekler
//   (rotation + yeniden kullanım tespiti: çifte refresh tüm oturumları kapatır).
// - Refresh başarısızsa oturum temizlenir ve onSessionExpired dinleyicisi çağrılır.

import type { ApiEnvelope, TokenResponse } from './types'

const BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? ''
const REFRESH_KEY = 'ajandaai.refreshToken'

export class ApiError extends Error {
  status: number
  errors: string[]

  constructor(status: number, message: string, errors: string[] = []) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

let accessToken: string | null = null
let refreshPromise: Promise<boolean> | null = null
let sessionExpiredListener: (() => void) | null = null

export function getRefreshToken(): string | null {
  try {
    return localStorage.getItem(REFRESH_KEY)
  } catch {
    return null
  }
}

function setRefreshToken(token: string | null): void {
  try {
    if (token) localStorage.setItem(REFRESH_KEY, token)
    else localStorage.removeItem(REFRESH_KEY)
  } catch {
    // localStorage kapalıysa oturum yalnızca bu sekmede yaşar.
  }
}

export function setSession(tokens: TokenResponse): void {
  accessToken = tokens.accessToken
  setRefreshToken(tokens.refreshToken)
}

export function clearSession(): void {
  accessToken = null
  setRefreshToken(null)
}

export function onSessionExpired(listener: (() => void) | null): void {
  sessionExpiredListener = listener
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  query?: Record<string, string | number | undefined>
  // false: token eklenmez ve 401'de refresh denenmez (login, register, refresh...).
  auth?: boolean
}

interface RawResult<T> {
  status: number
  envelope: ApiEnvelope<T> | null
}

async function send<T>(path: string, options: RequestOptions): Promise<RawResult<T>> {
  const url = new URL(path, BASE_URL || window.location.origin)
  for (const [key, value] of Object.entries(options.query ?? {})) {
    if (value !== undefined) url.searchParams.set(key, String(value))
  }
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (options.auth !== false && accessToken) headers.Authorization = `Bearer ${accessToken}`

  let response: Response
  try {
    response = await fetch(url, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'Sunucuya ulaşılamadı. İnternet bağlantınızı ve sunucunun çalıştığını kontrol edin.')
  }

  let envelope: ApiEnvelope<T> | null = null
  if (response.status !== 204) {
    try {
      envelope = (await response.json()) as ApiEnvelope<T>
    } catch {
      envelope = null
    }
  }
  return { status: response.status, envelope }
}

// Eşzamanlı çağrılar tek bir refresh isteğini paylaşır. Başarılıysa true döner.
export function refreshAccessToken(): Promise<boolean> {
  if (refreshPromise) return refreshPromise
  const refreshToken = getRefreshToken()
  if (!refreshToken) return Promise.resolve(false)

  refreshPromise = (async () => {
    try {
      const { status, envelope } = await send<TokenResponse>('/api/auth/refresh', {
        method: 'POST',
        body: { refreshToken },
        auth: false,
      })
      if (status >= 200 && status < 300 && envelope?.success && envelope.data) {
        setSession(envelope.data)
        return true
      }
      if (status === 401 || status === 400) clearSession()
      return false
    } catch {
      // Ağ hatası: oturumu silme, kullanıcı tekrar deneyebilsin.
      return false
    } finally {
      refreshPromise = null
    }
  })()
  return refreshPromise
}

function toError(status: number, envelope: ApiEnvelope<unknown> | null): ApiError {
  if (status === 429) {
    return new ApiError(429, 'Çok fazla deneme yaptınız. Lütfen bir dakika bekleyip tekrar deneyin.')
  }
  const message = envelope?.message || 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
  return new ApiError(status, message, envelope?.errors ?? [])
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let result = await send<T>(path, options)

  if (result.status === 401 && options.auth !== false) {
    const refreshed = await refreshAccessToken()
    if (refreshed) {
      result = await send<T>(path, options)
    }
    if (!refreshed || result.status === 401) {
      clearSession()
      sessionExpiredListener?.()
      throw new ApiError(401, 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.')
    }
  }

  const { status, envelope } = result
  if (status < 200 || status >= 300 || (envelope && !envelope.success)) {
    throw toError(status, envelope)
  }
  return (envelope?.data ?? undefined) as T
}
