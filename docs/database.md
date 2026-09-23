# Database

## Veritabanı
PostgreSQL. Docker Compose ile ayağa kaldırılır.
EF Core, Code First yaklaşımı.

## Enum Saklama
Tüm enum'lar veritabanında STRING olarak saklanır.
HasConversion<string>() kullanılır. Int saklama yasak.

## DbContext Kuralları
AppDbContext tek yerdedir: Infrastructure/Persistence/AppDbContext.cs

Entity yapılandırmaları AppDbContext içine YAZILMAZ.
Her entity kendi IEntityTypeConfiguration<T> dosyasını alır:
  Infrastructure/Persistence/Configurations/{Entity}Configuration.cs

AppDbContext sadece şunu çağırır:
  modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

Bu kural, birden fazla agent'ın aynı dosyada çakışmasını önlemek içindir.

## Migration Kuralları — KRİTİK
Migration üretme yetkisi TEK bir agent'a aittir: db-agent

Diğer tüm agent'lar için YASAK komutlar:
- dotnet ef migrations add
- dotnet ef migrations remove
- dotnet ef database update

Migration ihtiyacı olan agent, kendi migration'ını üretmez.
Bunun yerine takım lideri veya db-agent'a bildirir.

Gerekçe: Migration dosyaları zaman damgalı ve zincirlemedir.
Paralel üretilen iki migration, birbirini tanımayan iki dal oluşturur
ve __EFMigrationsHistory tablosunu bozar.

## Migration İsimlendirme
Add{Entity}, Update{Entity}{Alan}, Remove{Entity} formatında.
Örnek: AddActivity, AddCategoryLookup
