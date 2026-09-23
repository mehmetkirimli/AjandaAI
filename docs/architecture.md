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
src/AjandaAI.Domain/Entities/
src/AjandaAI.Domain/Enums/
src/AjandaAI.Application/Interfaces/
src/AjandaAI.Application/Services/
src/AjandaAI.Application/DTOs/
src/AjandaAI.Infrastructure/Persistence/
src/AjandaAI.Infrastructure/Persistence/Configurations/
src/AjandaAI.Infrastructure/Repositories/
src/AjandaAI.Api/Controllers/

## Bağımlılık Kuralı
Referanslar yukarıdaki yönde akar. Ters yönde referans YASAK.
Domain hiçbir şeye bağımlı değildir.

## Tasarım Felsefesi
Basit başla. Pattern, somut bir ihtiyaç doğduğunda eklenir.
Spekülatif soyutlama yasak.
Aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarma (Rule of Three).
Generic repository, outbox pattern gibi yapılar v1'de KULLANILMAZ.
