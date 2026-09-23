# Commands

## Build
dotnet build                          → tüm solution'ı derler
dotnet build --nologo                 → sade çıktı
dotnet clean                          → build çıktılarını siler
dotnet restore                        → paketleri geri yükler

## Test
dotnet test                           → tüm testleri çalıştırır
dotnet test --filter "FullyQualifiedName~Activity"  → sadece Activity testleri

## Çalıştırma
dotnet run --project src/AjandaAI.Api
Swagger: http://localhost:5000/swagger

## Docker / PostgreSQL
docker compose up -d                  → Postgres'i ayağa kaldırır
docker compose down                   → durdurur
docker compose down -v                → durdurur ve VERİYİ SİLER (dikkat)
docker compose logs -f postgres       → log takibi

## Migration — SADECE db-agent
dotnet ef migrations add {Ad} --project src/AjandaAI.Infrastructure --startup-project src/AjandaAI.Api
dotnet ef database update --project src/AjandaAI.Infrastructure --startup-project src/AjandaAI.Api
dotnet ef migrations list --project src/AjandaAI.Infrastructure --startup-project src/AjandaAI.Api

Diğer agent'lar bu komutları ÇALIŞTIRAMAZ. Gerekçe: docs/database.md

## Doğrulama Zinciri
Her değişiklikten sonra sırasıyla:
1. dotnet build   → 0 warning / 0 error olmalı
2. dotnet test    → tüm testler geçmeli

Bu zincir kırıksa iş tamamlanmış sayılmaz.
