import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import { getRefreshToken, onSessionExpired } from '../api/client'
import type { Me } from '../api/types'
import { AuthContext, type AuthState } from './context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [user, setUser] = useState<Me | null>(null)
  // Refresh token varsa açılışta sessiz oturum yenileme yapılır; o sırada "loading".
  const [loading, setLoading] = useState<boolean>(() => getRefreshToken() !== null)

  useEffect(() => {
    onSessionExpired(() => {
      setUser(null)
      queryClient.clear()
    })
    return () => onSessionExpired(null)
  }, [queryClient])

  useEffect(() => {
    if (!getRefreshToken()) return
    let cancelled = false
    // refreshAccessToken tek uçuşludur: StrictMode çift çalıştırması aynı isteği paylaşır.
    authApi
      .restoreSession()
      .then((ok) => (ok ? authApi.fetchMe() : null))
      .catch(() => null)
      .then((me) => {
        if (cancelled) return
        setUser(me)
        setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    await authApi.login(email, password)
    setUser(await authApi.fetchMe())
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
    queryClient.clear()
  }, [queryClient])

  const value = useMemo<AuthState>(() => ({ user, loading, login, logout }), [user, loading, login, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
