# Commands

## Build
dotnet build                          → tüm solution'ı derler
dotnet build --nologo                 → sade çıktı
dotnet clean                          → build çıktılarını siler
dotnet restore                        → paketleri geri yükler

## Test
dotnet test                                    → hepsi (unit + entegrasyon)
dotnet test tests/AjandaAI.Tests               → sadece unit testler (sahte repository, DB gerekmez)
dotnet test tests/AjandaAI.IntegrationTests    → sadece entegrasyon testleri (gerçek HTTP + PostgreSQL)
dotnet test --filter "FullyQualifiedName~Activity"  → sadece Activity testleri

### Entegrasyon testleri
ÖN KOŞUL: Docker ayakta olmalı (docker compose up -d) ve ajandaai_test
veritabanı mevcut olmalı (bkz. "Test veritabanı"). Postgres kapalıysa
entegrasyon testlerinin hepsi bağlantı hatasıyla kırılır; unit testler etkilenmez.

- API, WebApplicationFactory<Program> ile bellekte "Test" ortamında açılır → ajandaai_test.
- Bağlanılan veritabanı ajandaai_test değilse testler hiçbir şey silmeden durur.
- Her test sınıfı başlamadan: bekleyen migration'lar uygulanır, ardından
  TRUNCATE reminders, activities, users RESTART IDENTITY CASCADE.
  Seed kategoriler (categories) korunur.
- Sınıflar aynı veritabanını paylaştığı için paralel çalışma kapalıdır.
- MigrationTests geçici bir ajandaai_migration_{guid} veritabanı açıp tüm
  migration'ları baştan uygular ve test sonunda siler.

## Çalıştırma
dotnet run --project src/AjandaAI.Api
Swagger: http://localhost:5000/swagger

## Ortam Yönetimi

### Hangi dosya yüklenir
appsettings.json her zaman yüklenir (Logging, AllowedHosts — connection string YOK).
Üstüne ortam adına göre appsettings.{Ortam}.json biner:

| Ortam       | Dosya                         | Veritabanı     | Git'te |
|-------------|-------------------------------|----------------|--------|
| Development | appsettings.Development.json  | ajandaai       | evet   |
| Test        | appsettings.Test.json         | ajandaai_test  | evet   |
| Production  | appsettings.Production.json   | (gizli)        | HAYIR  |

Başlangıçta şu satır loglanır (şifre loglanmaz):
  Ortam: Development, Veritabanı: ajandaai

### Ortam adı nereden gelir (öncelik sırası, yüksekten düşüğe)
1. Komut satırı argümanı: --environment {Ortam}
2. DOTNET_ENVIRONMENT
3. ASPNETCORE_ENVIRONMENT
4. Hiçbiri yoksa: Production

DİKKAT: Bu proje WebApplication.CreateBuilder kullanır. Bu modelde
DOTNET_ENVIRONMENT, ASPNETCORE_ENVIRONMENT'ı EZER (klasik WebHost'un tersi).
Bu makinede doğrulandı: DOTNET_ENVIRONMENT=Development tanımlıyken
ASPNETCORE_ENVIRONMENT=Test vermek uygulamayı Development'ta açar.

Bu makinede DOTNET_ENVIRONMENT=Development kalıcı olarak tanımlıdır;
bu yüzden argümansız çalıştırma Development'a düşer.

### Test ortamında çalıştırma
dotnet run --project src/AjandaAI.Api -- --environment Test

"--" ŞART: SDK 9'da `dotnet run` kendi -e/--environment seçeneğini tanır
(environment variable atamak için); "--" olmadan argüman uygulamaya ulaşmaz.

Test veritabanına migration:
  dotnet ef database update --project src/AjandaAI.Infrastructure --startup-project src/AjandaAI.Api -- --environment Test

### Production
appsettings.Production.json git'e GİRMEZ (.gitignore). Şablon:
src/AjandaAI.Api/appsettings.Production.example.json (placeholder değerler).
Gerçek değerler dosyaya değil, sunucuda environment variable olarak verilmelidir:
  ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
  DOTNET_ENVIRONMENT=Production

### Test veritabanı
ajandaai_test, ajandaai ile aynı Postgres sunucusundadır.
docker/postgres-init/01-create-test-db.sql yalnızca BOŞ volume ile ilk
başlatmada çalışır. Mevcut volume'da elle oluşturmak için:
  docker exec ajandaai-postgres psql -U ajandaai -d ajandaai -c "CREATE DATABASE ajandaai_test OWNER ajandaai;"

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

## Bilinen Ortam Sorunları

dotnet-ef PATH'te olmayabilir. Tam yol:
%USERPROFILE%\.dotnet\tools\dotnet-ef.exe
Kalıcı çözüm: bu klasörü kullanıcı PATH'ine ekleyin.

Build almadan önce çalışan AjandaAI.Api process'ini kapatın.
Çalışan uygulama bin/ altındaki DLL'leri kilitler ve
build "dosya kopyalanamadı" hatası verir.
