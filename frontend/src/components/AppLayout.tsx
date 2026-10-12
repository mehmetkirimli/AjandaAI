import { Box, Drawer } from '@mantine/core'
import { useHotkeys, useLocalStorage, useMediaQuery, useDisclosure } from '@mantine/hooks'
import { useMemo } from 'react'
import { Outlet } from 'react-router-dom'
import { LayoutContext, type LayoutState } from '../layout/context'
import { ActivityFormProvider } from './activity-form/ActivityFormProvider'
import { CommandPalette } from './CommandPalette'
import { Sidebar } from './Sidebar'

const SIDEBAR_WIDTH = 232

// Linear tarzı yerleşim: solda sabit sidebar, sağda 8 px boşluklu inset panel.
// Panelin üst barı ve içeriği sayfaların kendisindedir (PanelHeader + PageBody).
export function AppLayout() {
  const isMobile = useMediaQuery('(max-width: 899px)') ?? false
  const [collapsed, setCollapsed] = useLocalStorage<boolean>({
    key: 'ajandaai.sidebarCollapsed',
    defaultValue: false,
    getInitialValueInEffect: false,
  })
  const [mobileOpen, { open: openMobile, close: closeMobile }] = useDisclosure(false)

  // "[" ile daralt/aç (input odaktayken çalışmaz).
  useHotkeys([['[', () => setCollapsed((c) => !c)]])

  const layout = useMemo<LayoutState>(
    () => ({ collapsed, toggleSidebar: () => setCollapsed((c) => !c), isMobile, openMobileNav: openMobile }),
    [collapsed, isMobile, setCollapsed, openMobile],
  )

  const showSidebar = !isMobile && !collapsed

  return (
    <LayoutContext.Provider value={layout}>
      <ActivityFormProvider>
      <CommandPalette />
      <Box style={{ display: 'flex', height: '100vh', background: 'var(--app-bg)' }}>
        {showSidebar && (
          <Box
            component="nav"
            aria-label="Ana menü"
            style={{
              width: SIDEBAR_WIDTH,
              flex: 'none',
              background: 'var(--app-layer-1)',
              backdropFilter: 'blur(12px)',
              WebkitBackdropFilter: 'blur(12px)',
              borderRight: '1px solid var(--app-line)',
            }}
          >
            <Sidebar />
          </Box>
        )}
        <Box
          component="main"
          style={{ flex: 1, minWidth: 0 }}
        >
          <Box
            style={{
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
              background: 'var(--mantine-color-body)',
            }}
          >
            <Outlet />
          </Box>
        </Box>
      </Box>
      {isMobile && (
        <Drawer
          opened={mobileOpen}
          onClose={closeMobile}
          size={260}
          withCloseButton={false}
          styles={{ content: { background: 'var(--app-layer-1)', backdropFilter: 'blur(12px)' }, body: { padding: 0, height: '100%' } }}
        >
          <Sidebar onNavigate={closeMobile} />
        </Drawer>
      )}
      </ActivityFormProvider>
    </LayoutContext.Provider>
  )
}
