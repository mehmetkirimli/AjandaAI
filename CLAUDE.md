# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proje

AjandaAI, .NET 8 üzerinde katmanlı mimari ile kurulmuş bir ajanda/takvim uygulamasıdır.
v1 (CRUD, sayfalama, log) ve v2 Authentication (JWT, sahiplik, admin) tamamlandı.
Sıradaki iş web frontend'dir (`frontend/`). Nerede olduğumuz ve sıra: [docs/roadmap.md](docs/roadmap.md).

## Stack

- Backend: .NET 8 (`net8.0`) — makinede kurulu SDK: 9.0.102. ASP.NET Core Web API + Swagger,
  EF Core + PostgreSQL, Serilog → MongoDB, xUnit.
- Frontend (ADR 0021): TypeScript + React (Vite), Mantine, FullCalendar, TanStack Query. Node 20+.

## Yapı

Backend ve frontend ayrı klasörlerdedir (ADR 0021 Karar 6). Komutlar repo kökünden çalışır;
kökte `.sln` yoktur: `dotnet build backend`, `dotnet test backend` (bkz. docs/commands.md).

```
backend/
  AjandaAI.sln
  src/
    AjandaAI.Domain/          classlib
    AjandaAI.Application/     classlib  -> Domain
    AjandaAI.Infrastructure/  classlib  -> Application
    AjandaAI.Api/             webapi    -> Application, Infrastructure
  tests/
    AjandaAI.Tests/              xunit  -> Application, Domain (unit, sahte repository)
    AjandaAI.IntegrationTests/   xunit  -> Api (gerçek HTTP + PostgreSQL)
frontend/                     React istemcisi (ADR 0021)
docker/, docker-compose.yml   PostgreSQL + MongoDB (ortak altyapı)
docs/                         dokümantasyon ve ADR'ler
```

Referans yönü tek yönlüdür ve yukarıdaki şemanın dışına çıkılmaz.
Api → Infrastructure referansı **composition root desenidir**: Api, Infrastructure'ın
iç sınıflarını kullanmaz, yalnızca `AddInfrastructure(...)` (ve Development'ta
`AddDevelopmentInfrastructure()`) extension'larını çağırır.
Bu referansı kaldırmayın — detay için [docs/architecture.md](docs/architecture.md).

## Dokümantasyon

Detaylar `docs/` altındadır — ilgili konuda çalışmadan önce o dosyayı oku:

- [docs/roadmap.md](docs/roadmap.md) — aşamalar, durum, sıradaki adımlar
- [docs/architecture.md](docs/architecture.md) — katman sorumlulukları, bağımlılık kuralları
- [docs/conventions.md](docs/conventions.md) — kod stili, isimlendirme, klasör düzeni, paylaşılan dosyalar
- [docs/database.md](docs/database.md) — veri erişimi, migration, şema
- [docs/commands.md](docs/commands.md) — build, test, run komutları
- [docs/domain.md](docs/domain.md) — iş alanı kavramları ve kuralları
- [docs/auth-test-senaryolari.md](docs/auth-test-senaryolari.md) — auth kabul kriterleri (AUTH-xx)
- [docs/decisions/](docs/decisions/README.md) — Architecture Decision Records: kararların
  NEDEN alındığı. Bir kuralı değiştirmeden önce ilgili ADR'yi oku.

## Agent çalışması

Agent'larla iş yapılırken kalite kapısı, review agent ve görev tanımı kuralları
[ADR 0020](docs/decisions/0020-kalite-kapisi-ve-review-agent.md)'dedir.
Backend agent'ları `backend/`, web-agent `frontend/` içinde çalışır.
