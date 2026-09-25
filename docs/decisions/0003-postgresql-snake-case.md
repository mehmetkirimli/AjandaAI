# 0003 - PostgreSQL snake_case isimlendirme
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
EF Core, C# PascalCase isimlerini tablo/kolon adı olarak olduğu gibi kullanır
(`"Activities"."CreatedAt"`). Veritabanı PostgreSQL'dir.

## Karar
Tüm tablo ve kolon adları snake_case saklanır. `EFCore.NamingConventions` paketi ve
`UseSnakeCaseNamingConvention()` ile otomatik uygulanır. C# tarafı PascalCase kalır.
Configuration dosyalarında `ToTable()` / `HasColumnName()` ile elle isim vermek yasaktır.

## Gerekçe
"PostgreSQL tırnaksız tanımlayıcıları küçük harfe çevirir. PascalCase kolonlar her sorguda
çift tırnak gerektirir." (docs/database.md)

Elle isim vermenin yasak olması:
- Elle isim vermek convention'ı kısmen bozar: bir alanda manuel isim, diğerlerinde otomatik
  isim olur ve şema tutarsızlaşır.
- Tutarsız şemada geliştirici hangi kolonun tırnaklanması gerektiğini bilemez; sorgu yazmak
  hataya açık hale gelir.

## Sonuçlar
- Raw SQL ve psql sorguları tırnaksız yazılabilir (`activities.created_at`).
- İsimlendirme tek noktadan, otomatik yönetilir.
- Kısıt: convention dışında özel bir isim gerektiğinde istisna mekanizması yok.
- Raw SQL yazarken (ör. `ix_users_email_lower`) snake_case adların elle doğru yazılması gerekir.
