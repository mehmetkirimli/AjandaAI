import '@fontsource-variable/inter'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/spotlight/styles.css'
import './theme.css'

import { DatesProvider } from '@mantine/dates'
import { MantineProvider, createTheme, type MantineColorsTuple } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import { AuthProvider } from './auth/AuthContext'

// Tek vurgu: Linear indigo'su (#5E6AD2). Yalnızca birincil buton, odak, "bugün" rozeti ve şimdi çizgisi.
const accent: MantineColorsTuple = [  '#f0f1fc',  '#dfe2f7',  '#c0c5ee',  '#a1a8e5',  '#818bdb',  '#5e6ad2',  '#5560c0',  '#4a54a8',  '#3f4890',  '#343c78',]

// Koyu palet: 7 = zemin, 6 = yüzey, 5 = hover/yüzey-2, 4 = çerçeve; 0-3 = metin kademeleri.
const dark: MantineColorsTuple = [  '#ededef',  '#c9c9ce',  '#8d8d96',  '#6b6b74',  '#26272b',  '#1b1c21',  '#15161a',  '#101114',  '#0b0c0e',  '#08090a',]

const theme = createTheme({
  primaryColor: 'accent',
  primaryShade: { light: 5, dark: 5 },
  colors: { accent, dark },
  // Kompakt ölçek: rem tabanlı her şey %90; taban yazı ~13 px.
  scale: 0.9,
  fontFamily: "'Inter Variable', Inter, 'Segoe UI', system-ui, -apple-system, sans-serif",
  fontSizes: { xs: '0.8rem', sm: '0.9rem', md: '0.95rem', lg: '1.05rem', xl: '1.2rem' },
  headings: {
    fontFamily: "'Inter Variable', Inter, 'Segoe UI', system-ui, sans-serif",
    fontWeight: '600',
    sizes: {
      h1: { fontSize: '1.9rem', lineHeight: '1.2' },
      h2: { fontSize: '1.5rem', lineHeight: '1.25' },
      h3: { fontSize: '1.3rem', lineHeight: '1.3' },
      h4: { fontSize: '1.05rem', lineHeight: '1.35' },
      h5: { fontSize: '0.95rem', lineHeight: '1.4' },
      h6: { fontSize: '0.85rem', lineHeight: '1.4' },
    },
  },
  defaultRadius: 'md',
  radius: { xs: '4px', sm: '6px', md: '8px', lg: '10px', xl: '14px' },
  // Gölge neredeyse yok; yalnızca popover, drawer ve sürükleme katmanında geniş ve yumuşak.
  shadows: {
    xs: 'none',
    sm: 'none',
    md: '0 8px 30px rgba(0, 0, 0, 0.18)',
    lg: '0 16px 48px rgba(0, 0, 0, 0.28)',
    xl: '0 24px 64px rgba(0, 0, 0, 0.4)',
  },
  components: {
    Alert: { defaultProps: { variant: 'default' } },
    Title: { styles: { root: { letterSpacing: '-0.02em' } } },
    Button: { defaultProps: { size: 'sm' }, styles: { root: { fontWeight: 500 } } },
    TextInput: { defaultProps: { size: 'sm' } },
    PasswordInput: { defaultProps: { size: 'sm' } },
    Select: { defaultProps: { size: 'sm' } },
    ActionIcon: { defaultProps: { size: 'md', variant: 'subtle', color: 'gray' } },
    Paper: { styles: { root: { background: 'var(--app-surface)', borderColor: 'var(--app-line)' } } },
    Drawer: { styles: { content: { background: 'var(--app-surface)' }, header: { background: 'var(--app-surface)' } } },
    Menu: { styles: { dropdown: { background: 'var(--app-surface)', boxShadow: 'var(--app-popover-shadow)' } } },
    // Rozet alt sınırı 11 px (okunabilirlik).
    Badge: {
      styles: {
        root: { fontWeight: 500, textTransform: 'none', '--badge-fz': '11px', '--badge-height': '18px' },
      },
    },
    Notification: { styles: { root: { background: 'var(--app-surface-2)', borderColor: 'var(--app-line-strong)' } } },
  },
})

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme} defaultColorScheme="dark">
      <DatesProvider settings={{ locale: 'tr', firstDayOfWeek: 1 }}>
      <Notifications position="bottom-right" />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
      </DatesProvider>
    </MantineProvider>
  </StrictMode>,
)
