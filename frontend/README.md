# AjandaAI Frontend

React + TypeScript (Vite) web istemcisi. Yığın: Mantine, TanStack Query, React Router, dnd-kit (ADR 0021).

## Çalıştırma

1. Backend'i başlat (repo kökünden):
   ```
   docker compose up -d
   dotnet run --project backend/src/AjandaAI.Api --launch-profile http
   ```
   API `http://localhost:5250` adresinde çalışır.
2. Frontend:
   ```
   cd frontend
   npm install
   npm run dev
   ```
   Uygulama `http://localhost:5173` adresinde açılır (port sabittir, backend CORS ve doğrulama linki bu adrese ayarlı).

Kayıt sonrası doğrulama bağlantısı e-posta ile gitmez; backend konsol log'unda
`http://localhost:5173/verify-email?token=...` olarak yazılır. Linki tarayıcıda aç.

## Komutlar

- `npm run dev` geliştirme sunucusu
- `npm run build` TypeScript kontrolü + üretim derlemesi
- `npm run lint` lint (oxlint)

## Ortam

`.env.development` içinde `VITE_API_BASE_URL=http://localhost:5250`.
