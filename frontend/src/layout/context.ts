import { createContext, useContext } from 'react'

export interface LayoutState {
  // Mantine `sm` altı (<768 px): sidebar yerine alt bar + yüzen "+" butonu.
  isMobile: boolean
  // Mobilde tam menü (yönetim, hesap) alt bardaki "Menü" ile drawer olarak açılır.
  openMobileNav: () => void
}

export const LayoutContext = createContext<LayoutState | null>(null)

export function useLayout(): LayoutState {
  const ctx = useContext(LayoutContext)
  if (!ctx) throw new Error('useLayout, AppLayout içinde kullanılmalı.')
  return ctx
}
