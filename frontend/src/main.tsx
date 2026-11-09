import '@fontsource/kalam/400.css'
import '@fontsource/kalam/700.css'
import '@fontsource-variable/inter'
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

// "Eskiz Defter" (ADR 0021 Karar 5f). Renk değerleri theme.css'teki token'larla aynıdır.
// Birincil: grafit (#2b2a27, shade 9). Dolu butonda açık metin.
const graphite: MantineColorsTuple = ['#f2f1ed', '#e4e3df', '#cfcec9', '#bdbcb6', '#a3a29c', '#8a8984', '#6b6a65', '#5c5b56', '#3d3c38', '#2b2a27']
// Solgun kategori tonları. Mantine'in renkli adları (green, teal, orange...) bunlara eşlenir; arayüzde parlak renk kalmaz.
const sage: MantineColorsTuple = ['#eef2ed', '#dde6db', '#c3d3c0', '#a6bea2', '#8aa886', '#6f8f6a', '#5d7a58', '#4b6347', '#3a4d37', '#2a3828']
const petrol: MantineColorsTuple = ['#ecf3f3', '#d8e6e6', '#b9d2d2', '#97baba', '#78a2a2', '#5e8a8a', '#4e7474', '#3f5e5e', '#304848', '#223333']
const lavender: MantineColorsTuple = ['#efeff6', '#dedded', '#c4c2dc', '#a9a7ca', '#918eb8', '#7d7aa6', '#69668f', '#555376', '#42405c', '#2f2e42']
const terra: MantineColorsTuple = ['#f6efea', '#ecdcd1', '#dcc0ad', '#cba286', '#bd8b6a', '#b07a5a', '#99664a', '#7d523b', '#62402e', '#472e21']
// Hata: koyu terrakota (theme.css --danger = shade 6).
const danger: MantineColorsTuple = ['#f8ece8', '#efd5cc', '#e0b1a2', '#cf8a74', '#bf6a50', '#b0553a', '#9b3f22', '#82341c', '#682916', '#4e1f10']

const theme = createTheme({
  primaryColor: 'graphite',
  primaryShade: 9,
  colors: {
    graphite,
    gray: graphite,
    dark: graphite,
    green: sage,
    teal: petrol,
    cyan: petrol,
    blue: petrol,
    violet: lavender,
    grape: lavender,
    orange: terra,
    yellow: terra,
    red: danger,
  },
  white: '#fcfcfa',
  black: '#2b2a27',
  fontFamily: "'Inter Variable', 'Inter', system-ui, -apple-system, sans-serif",
  fontFamilyMonospace: "'JetBrains Mono Variable', 'JetBrains Mono', ui-monospace, monospace",
  fontSizes: { xs: '12px', sm: '13px', md: '14px', lg: '16px', xl: '18px' },
  headings: {
    fontFamily: "'Kalam', cursive",
    fontWeight: '700',
    sizes: {
      h1: { fontSize: '32px', lineHeight: '1.1' },
      h2: { fontSize: '26px', lineHeight: '1.15' },
      h3: { fontSize: '21px', lineHeight: '1.2' },
      h4: { fontSize: '19px', lineHeight: '1.25' },
      h5: { fontSize: '16px', lineHeight: '1.3' },
      h6: { fontSize: '14px', lineHeight: '1.35' },
    },
  },
  defaultRadius: 'sm',
  radius: { xs: '3px', sm: '4px', md: '6px', lg: '8px', xl: '12px' },
  shadows: {
    xs: 'none',
    sm: '3px 3px 0 rgba(43, 42, 39, 0.12)',
    md: '3px 3px 0 rgba(43, 42, 39, 0.12)',
    lg: '5px 5px 0 rgba(43, 42, 39, 0.14)',
    xl: '5px 5px 0 rgba(43, 42, 39, 0.14)',
  },
  focusRing: 'auto',
  components: {
    Alert: { defaultProps: { variant: 'light' } },
    Button: { styles: { root: { fontWeight: 600 } } },
    ActionIcon: { defaultProps: { variant: 'subtle', color: 'graphite' } },
    Badge: { defaultProps: { variant: 'outline' }, styles: { root: { fontWeight: 600, textTransform: 'none' } } },
    Paper: { styles: { root: { background: 'var(--paper)', border: 'var(--pen)', boxShadow: 'var(--sketch-shadow)' } } },
    Modal: { defaultProps: { overlayProps: { backgroundOpacity: 0.25, color: '#2b2a27' } } },
    Drawer: { defaultProps: { overlayProps: { backgroundOpacity: 0.25, color: '#2b2a27' } } },
  },
})

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Eskiz Defter tek temadır (açık). */}
    <MantineProvider theme={theme} forceColorScheme="light">
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
