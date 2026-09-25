# AjandaAI

Kişisel asistan / ajanda uygulaması. Kullanıcılar sosyal hayatlarını planlar,
aktivite kaydeder, tamamlananları puanlar. v1 yalnızca bir REST API'dir; frontend yoktur,
API Swagger üzerinden test edilir.

## Teknoloji

.NET 8 · ASP.NET Core Web API · PostgreSQL 16 · EF Core (Code First, Npgsql) · Docker ·
xUnit · FluentValidation · Swagger

## Mimari

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure
```

- **Domain:** entity'ler ve enum'lar; hiçbir şeye bağımlı değildir.
- **Application:** servisler, DTO'lar, validator'lar, repository interface'leri.
- **Infrastructure:** EF Core, `AppDbContext`, repository implementasyonları.
- **Api:** controller'lar, filter'lar, middleware, `Program.cs`.

Referanslar yalnızca ok yönünde akar. Api → Infrastructure referansı **composition root**
desenidir: Api yalnızca `AddInfrastructure(...)` çağırır. Katman içinde kod kaynak bazlı
(dikey dilim) organize edilir.

Detay: [docs/architecture.md](docs/architecture.md)

## Hızlı Başlangıç

**1. Gereksinimler**
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (veya daha yeni bir SDK)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- EF Core CLI aracı:
  ```
  dotnet tool install --global dotnet-ef --version 8.*
  ```

**2. Veritabanını başlat** (PostgreSQL 16, host portu `5434`)
```
docker compose up -d
```

**3. Migration'ları uygula**
```
dotnet ef database update --project src/AjandaAI.Infrastructure --startup-project src/AjandaAI.Api
```

**4. Uygulamayı çalıştır** (Development ortamı)
```
dotnet run --project src/AjandaAI.Api --launch-profile http
```

**5. Swagger:** http://localhost:5250/swagger

Swagger yalnızca Development ortamında açıktır.

## Ortamlar

| Ortam       | Ayar dosyası                  | Veritabanı      | Git'te |
|-------------|-------------------------------|-----------------|--------|
| Development | `appsettings.Development.json` | `ajandaai`      | evet   |
| Test        | `appsettings.Test.json`        | `ajandaai_test` | evet   |
| Production  | `appsettings.Production.json`  | (gizli)         | hayır  |

Ortam adı şu öncelikle belirlenir: `--environment` argümanı > `DOTNET_ENVIRONMENT` >
`ASPNETCORE_ENVIRONMENT` > varsayılan `Production`. `DOTNET_ENVIRONMENT`,
`ASPNETCORE_ENVIRONMENT`'ı ezer.

Test ortamında çalıştırma (`--` şart):
```
dotnet run --project src/AjandaAI.Api -- --environment Test
```

Production'da connection string dosyaya değil environment variable'a yazılır
(`ConnectionStrings__DefaultConnection`). Şablon: `src/AjandaAI.Api/appsettings.Production.example.json`.

Detay: [docs/commands.md](docs/commands.md#ortam-yönetimi)

## API

Her kaynak aynı beş endpoint'i sunar:

| Kaynak     | Temel yol          | Liste filtreleri (opsiyonel)                             |
|------------|--------------------|----------------------------------------------------------|
| Users      | `/api/users`       | `search` (email veya görünen ad)                         |
| Categories | `/api/categories`  | —                                                        |
| Activities | `/api/activities`  | `from`, `to`, `status`, `categoryId`, `priority`         |
| Reminders  | `/api/reminders`   | `activityId`, `isSent`                                   |

| Metot    | Yol           | Açıklama                                        |
|----------|---------------|-------------------------------------------------|
| `GET`    | `/`           | Sayfalı liste (`?page=1&pageSize=20`)           |
| `GET`    | `/{id}`       | Tek kayıt                                       |
| `POST`   | `/`           | Oluştur (201)                                   |
| `PUT`    | `/{id}`       | Güncelle                                        |
| `DELETE` | `/{id}`       | Sil: Users/Categories/Activities soft, Reminders hard |

Sayfalama: `pageSize` varsayılan 20, en fazla 100. Geçersiz değerler hata yerine en yakın
geçerli değere çekilir.

Tüm yanıtlar aynı zarf içinde döner. HTTP status kodu body'de değil, yalnızca HTTP
yanıtında bulunur. Örnek, `GET /api/categories?page=1&pageSize=2`:

```json
{
  "success": true,
  "data": {
    "items": [
      { "id": 1, "name": "Yeme & İçme", "isActive": true },
      { "id": 2, "name": "Seyahat & Gezi", "isActive": true }
    ],
    "totalCount": 10,
    "page": 1,
    "pageSize": 2,
    "totalPages": 5,
    "hasPrevious": false,
    "hasNext": true
  },
  "message": "",
  "errors": []
}
```

Hata örneği (400):
```json
{ "success": false, "data": null, "message": "Doğrulama hatası.", "errors": ["..."] }
```

Enum'lar JSON'da ve query string'de adlarıyla yazılır (`"status": "Planned"`).
Kurallar: [docs/conventions.md](docs/conventions.md)

## Testler

```
dotnet test                                    # hepsi
dotnet test tests/AjandaAI.Tests               # unit testler (DB gerekmez)
dotnet test tests/AjandaAI.IntegrationTests    # entegrasyon testleri
```

Entegrasyon testleri gerçek PostgreSQL kullanır. Çalıştırmadan önce **Docker ayakta
olmalı** (`docker compose up -d`) ve `ajandaai_test` veritabanı mevcut olmalıdır.
Testler bu veritabanındaki tabloları temizler; farklı bir veritabanına bağlanılırsa hiçbir şey
silmeden durur. Mevcut bir Docker volume'unda test veritabanını oluşturmak için:

```
docker exec ajandaai-postgres psql -U ajandaai -d ajandaai -c "CREATE DATABASE ajandaai_test OWNER ajandaai;"
```

## Proje Yapısı

```
AjandaAI.sln
docker-compose.yml               PostgreSQL 16 (port 5434)
docker/postgres-init/            ilk başlatmada ajandaai_test veritabanını oluşturur
src/
  AjandaAI.Domain/               Entities/, Enums/
  AjandaAI.Application/
    Common/                      ApiResponse, PagedResult, PageRequest, IModule
    Users/ Categories/           her kaynak: servis, modül, repository interface'i,
    Activities/ Reminders/         Dtos/, Validators/
  AjandaAI.Infrastructure/
    Persistence/                 AppDbContext, Configurations/
    Repositories/                repository implementasyonları
    Migrations/                  EF Core migration'ları
    DependencyInjection.cs       AddInfrastructure(...)
  AjandaAI.Api/                  Controllers/, Filters/, Middleware/, Program.cs
tests/
  AjandaAI.Tests/                unit testler (sahte repository)
  AjandaAI.IntegrationTests/     HTTP + PostgreSQL entegrasyon testleri
docs/                            proje dokümantasyonu
.claude/                         agent tanımları ve migration hook'u
```

## Dokümantasyon

| Dosya | İçerik |
|-------|--------|
| [docs/architecture.md](docs/architecture.md) | Katmanlar, composition root, klasör düzeni, bağımlılık kuralları |
| [docs/conventions.md](docs/conventions.md) | İsimlendirme, ApiResponse/ResultType, doğrulama, silme politikası, sayfalama |
| [docs/database.md](docs/database.md) | EF Core kuralları, migration yetkisi, snake_case, index'ler |
| [docs/commands.md](docs/commands.md) | Build, test, run, ortam yönetimi, Docker, hook testi |
| [docs/domain.md](docs/domain.md) | Varlıklar, enum'lar, alanlar, kapsam |
| [docs/decisions/](docs/decisions/README.md) | Architecture Decision Records |

**docs/decisions/** mimari kararların **gerekçelerini** tutar: bir şeyin neden böyle
yapıldığını, hangi alternatiflerin reddedildiğini ve kararın bedelini. Bir kuralı
değiştirmeden önce ilgili ADR'yi okuyun.

> Migration üretme yetkisi yalnızca `db-agent`'a aittir ve bir hook ile zorlanır
> ([ADR 0008](docs/decisions/0008-migration-tekeli-ve-hook.md)). Bu kural Claude Code
> agent'ları içindir; kendi terminalinizde migration komutlarını çalıştırabilirsiniz.

## v1 Kapsamı ve Ertelenenler

**v1:** User, Category (lookup, 10 seed kategori), Activity, Reminder için CRUD API.
Aktiviteler durum (`Planned`, `InProgress`, `Completed`, `Cancelled`), öncelik, enerji
seviyesi, bütçe ve tamamlandıktan sonra 1-10 puan taşır. Authentication yoktur.

**v2'ye ertelenenler**
- Rating entity (analiz ajanı tarafından üretilecek)
- Plan / aktivite gruplama
- Ortak aktivite + davet sistemi (davet kodu ile)
- AiNote alanı
- Bildirim altyapısı
- MCP entegrasyonu
- Koordinat / harita
- Authentication ve kayıt sahipliği kontrolü

**v3'e ertelenenler**
- Aktivite fotoğrafları

Detay: [docs/domain.md](docs/domain.md)
