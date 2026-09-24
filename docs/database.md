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
Bunun yerine takım lideri (SendMessage, to: "main") veya db-agent'a bildirir.

Bu kural .claude/hooks/block-migrations.ps1 ile PreToolUse hook'u
olarak zorlanır. Hook kazara ihlali önleyen bir korkuluktur;
komutu dolaylı yoldan (script içinden) çağırmayı yakalamaz.

Gerekçe: Migration dosyaları zaman damgalı ve zincirlemedir.
Paralel üretilen iki migration, birbirini tanımayan iki dal oluşturur
ve __EFMigrationsHistory tablosunu bozar.

## Migration İsimlendirme
Add{Entity}, Update{Entity}{Alan}, Remove{Entity} formatında.
Örnek: AddActivity, AddCategoryLookup

## İsimlendirme — snake_case
Tüm tablo ve kolon adları PostgreSQL'de snake_case olarak saklanır.
EFCore.NamingConventions paketi + UseSnakeCaseNamingConvention() ile
otomatik uygulanır.

C# tarafında property'ler PascalCase kalır (Activity.CreatedAt),
veritabanında snake_case olur (activities.created_at).

Configuration dosyalarında ToTable() veya HasColumnName() ile
elle isim vermek YASAK. Convention bozulur.

Gerekçe: PostgreSQL tırnaksız tanımlayıcıları küçük harfe çevirir.
PascalCase kolonlar her sorguda çift tırnak gerektirir.

## Yetkilendirme Notu — v2
v1'de authentication yok. v2'de auth eklendiğinde, kullanıcıya ait
kayıtları dönen HER sorguda UserId kontrolü zorunludur.
Aksi halde IDOR açığı oluşur (/api/activities/6 ile başkasının
verisine erişim).
