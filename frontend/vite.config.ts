import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Port 5173 sabittir: backend CORS ve e-posta doğrulama linki bu adrese ayarlı.
// /api proxy'si LAN modu içindir (npm run dev:lan): ağdaki diğer bilgisayarlar API'ye
// Vite üzerinden ulaşır; backend localhost'ta kalır, CORS gerekmez.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: { '/api': 'http://localhost:5250' },
    // VS Code port yönlendirme (Dev Tunnels) adresinden gelen istekler.
    allowedHosts: ['.devtunnels.ms'],
  },
})
