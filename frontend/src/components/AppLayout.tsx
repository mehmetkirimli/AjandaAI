import { Box, Drawer, em } from '@mantine/core'
import { useDisclosure, useMediaQuery } from '@mantine/hooks'
import { useMemo } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { LayoutContext, type LayoutState } from '../layout/context'
import { ActivityFormProvider } from './activity-form/ActivityFormProvider'
import { CommandPalette } from './CommandPalette'
import { MobileNav } from './MobileNav'
import { Sidebar } from './Sidebar'
import '../layout.css'

// Mantine `sm` kırılımı (48em = 768 px).
export const MOBILE_QUERY = `(max-width: ${em(767.98)})`

// Eskiz Defter yerleşimi: masaüstünde hep açık sol sidebar; mobilde alt bar + ortada grafit "+".
export function AppLayout() {
  const isMobile = useMediaQuery(MOBILE_QUERY) ?? false
  const [mobileOpen, { open: openMobile, close: closeMobile }] = useDisclosure(false)
  const { pathname } = useLocation()

  const layout = useMemo<LayoutState>(() => ({ isMobile, openMobileNav: openMobile }), [isMobile, openMobile])

  return (
    <LayoutContext.Provider value={layout}>
      <ActivityFormProvider>
        <CommandPalette />
        <Box className="paper-bg app-shell">
          {!isMobile && (
            <nav aria-label="Ana menü" className="app-sidebar">
              <Sidebar />
            </nav>
          )}
          <main className="app-main" data-mobile={isMobile}>
            {/* key: her sayfa değişiminde yumuşak belirme/kayma. */}
            <div key={pathname} className="page-enter app-page">
              <Outlet />
            </div>
          </main>
        </Box>
        {isMobile && (
          <>
            <MobileNav onOpenMenu={openMobile} />
            <Drawer
              opened={mobileOpen}
              onClose={closeMobile}
              size={280}
              withCloseButton={false}
              styles={{ content: { background: 'var(--paper-side)' }, body: { padding: 0, height: '100%' } }}
            >
              <Sidebar onNavigate={closeMobile} />
            </Drawer>
          </>
        )}
      </ActivityFormProvider>
    </LayoutContext.Provider>
  )
}
