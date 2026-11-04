import '@fontsource-variable/bricolage-grotesque'
import '@fontsource/caveat/600.css'
import '@fontsource-variable/jetbrains-mono'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/spotlight/styles.css'
import './theme.css'

import { DatesProvider } from '@mantine/dates'
import { MantineProvider, createTheme, type MantineColorsTuple } from '@mantine/core'
import { ResponsiveNotifications } from './components/ResponsiveNotifications'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import { AuthProvider } from './auth/AuthContext'

// "Özel Defter" (ADR 0021 Karar 5e). Renk değerleri theme.css'teki token'larla aynıdır.
// Birincil: mor (#7C3AED, shade 6). Dolgu tonlarında metin beyaz.
const brand: MantineColorsTuple = ['#f3eefe', '#e4d9fd', '#c4b5fd', '#a78bfa', '#9061f9', '#8350f0', '#7c3aed', '#6d28d9', '#5b21b6', '#4c1d95']

// Koyu palet: 7 = zemin (#120d22), 6 = kart (#1b1530), 5 = yüzey-2, 4 = çerçeve; 0-3 = metin.
const dark: MantineColorsTuple = ['#ffffff', '#e9e5f5', '#c9c2dd', '#a59cc0', '#3a3156', '#2a2244', '#1b1530', '#120d22', '#0d0a19', '#08060f']

const theme = createTheme({
  primaryColor: 'brand',
  primaryShade: 6,
  colors: { brand, dark },
  white: '#ffffff',
  fontFamily: "'Bricolage Grotesque Variable', 'Bricolage Grotesque', system-ui, -apple-system, sans-serif",
  fontFamilyMonospace: "'JetBrains Mono Variable', 'JetBrains Mono', ui-monospace, monospace",
  fontSizes: { xs: '12px', sm: '14px', md: '15px', lg: '17px', xl: '20px' },
  headings: {
    fontFamily: "'Bricolage Grotesque Variable', 'Bricolage Grotesque', system-ui, sans-serif",
    fontWeight: '800',
    sizes: {
      h1: { fontSize: '30px', lineHeight: '1.15' },
      h2: { fontSize: '24px', lineHeight: '1.2' },
      h3: { fontSize: '20px', lineHeight: '1.25' },
      h4: { fontSize: '19px', lineHeight: '1.3', fontWeight: '700' },
      h5: { fontSize: '16px', lineHeight: '1.35', fontWeight: '700' },
      h6: { fontSize: '14px', lineHeight: '1.4', fontWeight: '700' },
    },
  },
  defaultRadius: 'md',
  radius: { xs: '6px', sm: '8px', md: '12px', lg: '16px', xl: '22px' },
  shadows: {
    xs: 'none',
    sm: '0 6px 16px rgba(0, 0, 0, 0.25)',
    md: '0 12px 30px rgba(0, 0, 0, 0.35)',
    lg: '0 20px 50px rgba(0, 0, 0, 0.45)',
    xl: '0 30px 80px rgba(0, 0, 0, 0.55)',
  },
  focusRing: 'auto',
  components: {
    Alert: { defaultProps: { variant: 'light', radius: 'md' } },
    Anchor: { defaultProps: { c: 'var(--l-teal)' } },
    Title: { styles: { root: { letterSpacing: '-0.02em' } } },
    Button: { defaultProps: { radius: 'md' }, styles: { root: { fontWeight: 700 } } },
    TextInput: { defaultProps: { radius: 'md' } },
    PasswordInput: { defaultProps: { radius: 'md' } },
    Select: { defaultProps: { radius: 'md' } },
    ActionIcon: { defaultProps: { variant: 'subtle', color: 'gray' } },
    Paper: { styles: { root: { background: 'var(--ink-card)', borderColor: 'var(--ink-line)' } } },
    Drawer: { styles: { content: { background: 'var(--ink-card)' }, header: { background: 'var(--ink-card)' } } },
    Modal: { styles: { content: { background: 'var(--ink-card)' }, header: { background: 'var(--ink-card)' } } },
    Menu: { styles: { dropdown: { background: 'var(--ink-card)', boxShadow: 'var(--app-popover-shadow)' } } },
    Badge: { styles: { root: { fontWeight: 700, textTransform: 'none' } } },
    Notification: { styles: { root: { background: 'var(--ink-card-2)', borderColor: 'var(--ink-line-strong)' } } },
  },
})

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Özel Defter yalnızca koyu temadır (taslaklar koyu); açık tema kaldırıldı. */}
    <MantineProvider theme={theme} forceColorScheme="dark">
      <DatesProvider settings={{ locale: 'tr', firstDayOfWeek: 1 }}>
      <ResponsiveNotifications />
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
