import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Port 5173 sabittir: backend CORS ve e-posta doğrulama linki bu adrese ayarlı.
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true },
})
