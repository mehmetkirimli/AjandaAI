import { createContext, useContext } from 'react'

export interface LayoutState {
  // Masaüstünde sidebar daraltılmış mı.
  collapsed: boolean
  toggleSidebar: () => void
  // <900 px: sidebar drawer olarak açılır.
  isMobile: boolean
  openMobileNav: () => void
}

export const LayoutContext = createContext<LayoutState | null>(null)

export function useLayout(): LayoutState {
  const ctx = useContext(LayoutContext)
  if (!ctx) throw new Error('useLayout, AppLayout içinde kullanılmalı.')
  return ctx
}
