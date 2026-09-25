# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proje

AjandaAI, .NET 8 üzerinde katmanlı mimari ile kurulmuş bir ajanda/takvim uygulamasıdır.
Solution şu an iskelet aşamasındadır: entity, controller ve DbContext henüz yazılmamıştır.
Katmanlar `src/` altında, testler `tests/` altında yer alır.

## Stack

- .NET 8 (`net8.0`) — makinede kurulu SDK: 9.0.102
- ASP.NET Core Web API + Swashbuckle (Swagger)
- xUnit

## Yapı

```
AjandaAI.sln
src/
  AjandaAI.Domain/          classlib
  AjandaAI.Application/     classlib  -> Domain
  AjandaAI.Infrastructure/  classlib  -> Application
  AjandaAI.Api/             webapi    -> Application, Infrastructure
tests/
  AjandaAI.Tests/           xunit     -> Application, Domain
```

Referans yönü tek yönlüdür ve yukarıdaki şemanın dışına çıkılmaz.
Api → Infrastructure referansı **composition root desenidir**: Api, Infrastructure'ın
iç sınıflarını kullanmaz, yalnızca `AddInfrastructure(...)` extension'ını çağırır.
Bu referansı kaldırmayın — detay için [docs/architecture.md](docs/architecture.md).

## Dokümantasyon

Detaylar `docs/` altındadır — ilgili konuda çalışmadan önce o dosyayı oku:

- [docs/architecture.md](docs/architecture.md) — katman sorumlulukları, bağımlılık kuralları
- [docs/conventions.md](docs/conventions.md) — kod stili, isimlendirme, klasör düzeni
- [docs/database.md](docs/database.md) — veri erişimi, migration, şema
- [docs/commands.md](docs/commands.md) — build, test, run komutları
- [docs/domain.md](docs/domain.md) — iş alanı kavramları ve kuralları
- [docs/decisions/](docs/decisions/README.md) — Architecture Decision Records: kararların
  NEDEN alındığı. Bir kuralı değiştirmeden önce ilgili ADR'yi oku.

> Bu dosyalar henüz doldurulmadı; içerikleri yazıldıkça burası referans kalır.
