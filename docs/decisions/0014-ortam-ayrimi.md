# 0014 - Ortam ayrımı (Development/Test/Production)
**Durum:** Kabul edildi
**Tarih:** 2026-09-25

## Bağlam
Uygulama geliştirme, entegrasyon testi ve üretimde farklı veritabanlarına bağlanmalı.
Entegrasyon testleri veritabanını TRUNCATE ediyor.

## Karar
- `appsettings.json` her zaman yüklenir ve connection string içermez.
- `appsettings.Development.json` → `ajandaai`, `appsettings.Test.json` → `ajandaai_test` (git'te).
- `appsettings.Production.json` git'e girmez; şablon `appsettings.Production.example.json`.
  Gerçek değerler sunucuda environment variable olarak verilir.
- Ortam adı önceliği: `--environment` > `DOTNET_ENVIRONMENT` > `ASPNETCORE_ENVIRONMENT` > Production.
- Başlangıçta ortam ve veritabanı adı loglanır (şifre loglanmaz).
- Entegrasyon testleri "Test" ortamında açılır; bağlanılan veritabanı `ajandaai_test`
  değilse hiçbir şey silmeden durur.

## Gerekçe
- Production sırları git'e girmemeli; gerçek değerler dosyada değil environment variable'da.
  (docs/commands.md)
- Entegrasyon testleri tabloları TRUNCATE ettiği için geliştirme verisinden ayrı bir
  veritabanı ve "yanlış veritabanıysa dur" korkuluğu gerekir.
- Hangi ortamda/veritabanında çalışıldığının başlangıç logunda görünmesi, yanlış ortama
  bağlanmayı fark etmeyi sağlar.

## Sonuçlar
- Testler geliştirme verisini silemez.
- Kısıt: `DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT`'ı ezer (WebApplication.CreateBuilder);
  bu makinede kalıcı `DOTNET_ENVIRONMENT=Development` tanımlı olduğundan Test için
  `dotnet run ... -- --environment Test` ("--" şart) kullanılmalıdır.
- Kısıt: `ajandaai_test` mevcut Docker volume'larında elle oluşturulmalıdır.
