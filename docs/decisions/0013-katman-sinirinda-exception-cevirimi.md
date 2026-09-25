# 0013 - Katman sınırında exception çevirimi
**Durum:** Kabul edildi
**Tarih:** 2026-09-25

## Bağlam
Email benzersizliği validator'da `EmailExistsAsync` ile kontrol ediliyor, ancak eşzamanlı
isteklerde iki istek aynı anda kontrolü geçip veritabanındaki unique index'e
(`ix_users_email_lower`) takılabiliyor. Bu durumda EF Core `DbUpdateException` /
Npgsql `PostgresException (23505)` fırlatır — Application bu tipleri tanımaz.

## Karar
- Infrastructure (`UserRepository.SaveChangesAsync`) Postgres 23505 hatasını yakalar ve
  Application'da tanımlı `DuplicateEmailException`'a çevirir.
- `UserService` bu exception'ı yakalar ve `ApiResponse.Conflict` (409) döner.
- Genel kural: servisler exception fırlatmaz, beklenen hatalar ResultType ile döner.
  Beklenmeyen exception'ları `ExceptionHandlingMiddleware` yakalar ve 500 döner.
- Exception mesajı ve stack trace yalnızca Development'ta `errors`'a eklenir.

## Gerekçe
- "Application EF Core'u tanımaz: Infrastructure, Postgres 23505 hatasını bu tipe çevirir."
  (DuplicateEmailException.cs) — bağımlılık kuralı (ADR 0001) korunur.
- "Validator'ın EmailExistsAsync kontrolü eşzamanlı isteklerde yarışı kaçırabilir;
  son savunma DB index'idir."
- Ham hata sızdırılmaz: hata mesajları iç tip adı, namespace, stack trace, dosya yolu
  içermez; Production'da ham hata gösterilmez. (docs/conventions.md)

## Sonuçlar
- Yarış durumunda istemci 500 yerine anlamlı bir 409 alır.
- Application altyapı tiplerinden bağımsız kalır.
- Kısıt: 23505 hatası doğrudan `DuplicateEmailException`'a çevrilir; hangi index'in ihlal
  edildiği kontrol edilmez. Bu, users tablosunda şu an tek bir unique index (email) bulunduğu
  için geçerlidir. İkinci bir unique kısıt eklenirse `PostgresException.ConstraintName`
  kontrol edilerek doğru exception'a çevrilmelidir.
- Kısıt: her yeni kısıt için ayrı domain exception tipi gerekir.
