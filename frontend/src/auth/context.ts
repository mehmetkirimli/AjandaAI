import { createContext, useContext } from 'react'
import type { Me } from '../api/types'

export interface AuthState {
  user: Me | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  // Profil güncellenince üst menüdeki bilgiyi tazelemek için.
  updateUser: (me: Me) => void
  // Sunucu oturumları zaten kapattıysa (şifre değişimi): yalnızca yerel oturumu temizler.
  endLocalSession: (notice?: string) => void
  // Giriş sayfasında gösterilecek bilgi mesajı (örn. şifre değişti).
  notice: string | null
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth, AuthProvider içinde kullanılmalı.')
  return ctx
}
