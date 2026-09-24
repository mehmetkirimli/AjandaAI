# Architecture

## Katmanlar
- AjandaAI.Domain        → Entity'ler, enum'lar. Hiçbir katmana referans vermez.
- AjandaAI.Application   → İş mantığı, servisler, DTO'lar, interface'ler. Domain'e referans verir.
- AjandaAI.Infrastructure → EF Core, DbContext, repository implementasyonları. Application'a referans verir.
- AjandaAI.Api           → Controller'lar, Program.cs. Application ve Infrastructure'a referans verir.
- AjandaAI.Tests         → xUnit. Application ve Domain'e referans verir.

## Composition Root
Api katmanı Infrastructure'a referans verir, ANCAK Infrastructure'ın
iç sınıflarını doğrudan kullanmaz.

Infrastructure kendi DI kayıtlarını bir extension method ile sunar:
  Infrastructure/DependencyInjection.cs
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)

Program.cs sadece şunu çağırır:
  builder.Services.AddInfrastructure(builder.Configuration);

Bu kasıtlı bir tasarımdır. Api'nin Infrastructure'a referansı "eksik" değildir,
composition root desenidir. Kaldırmayın.

## Klasör Düzeni
```
src/AjandaAI.Domain/
  Entities/
  Enums/

src/AjandaAI.Application/
  Common/                    (IModule, ApiResponse — paylaşılan, lead'in alanı)
  {Entity çoğulu}/           (Categories, Users, Activities, Reminders)
    I{Entity}Repository.cs
    {Entity}Service.cs
    {Entity}Module.cs
    Dtos/
    Validators/

src/AjandaAI.Infrastructure/
  Persistence/
    AppDbContext.cs
    Configurations/
  Repositories/
  DependencyInjection.cs

src/AjandaAI.Api/
  Controllers/
  Program.cs

tests/AjandaAI.Tests/
  {Entity çoğulu}/
```

Her kaynak (entity) kendi klasöründedir; klasör adı entity adının çoğuludur.
Örnek: Categories/ → ICategoryRepository, CategoryService, CategoryModule.
Bir klasör yalnızca tek bir entity'nin dosyalarını tutar.

## Klasör Kuralı
Kod KAYNAK bazlı organize edilir, katman bazlı değil.
Yani Application/Services/ veya Application/DTOs/ gibi klasörler AÇILMAZ.
Her kaynak kendi klasöründe tüm dosyalarını tutar.

Gerekçe: dikey dilim mimarisi. Bir agent tek klasörde çalışır,
çakışma yüzeyi küçülür.

Infrastructure/Repositories/ bunun istisnasıdır: tüm repository
implementasyonları burada toplanır (katman bazlı), çünkü hepsi
AppDbContext'e bağlıdır.

## Bağımlılık Kuralı
Referanslar yukarıdaki yönde akar. Ters yönde referans YASAK.
Domain hiçbir şeye bağımlı değildir.

## Kaynaklar Arası Bağımlılık
Dikey dilimler birbirine bağımlı olabilir, ancak bağımlılık TEK YÖNLÜ olmalıdır.
Yön, veritabanındaki foreign key yönüyle aynıdır.

İzin verilen:
- ActivityService / validator -> ICategoryRepository
- ActivityService / validator -> IUserRepository
- ReminderService / validator -> IActivityRepository

YASAK (döngü oluşturur):
- CategoryService -> IActivityRepository
- UserService -> IActivityRepository
- ActivityService -> IReminderRepository

Bir kaynak, kendisine FK veren kaynağı TANIMAZ.

## İlişkisel Doğrulama
Var olan bir kaydı referans alan alanlar (CategoryId, UserId, ActivityId)
FluentValidation validator'ında MustAsync ile doğrulanır.
Validator ilgili repository'yi constructor'dan alır.

Servis içinde tekrar kontrol edilmez.

## Tasarım Felsefesi
Basit başla. Pattern, somut bir ihtiyaç doğduğunda eklenir.
Spekülatif soyutlama yasak.
Aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarma (Rule of Three).
Generic repository, outbox pattern gibi yapılar v1'de KULLANILMAZ.
