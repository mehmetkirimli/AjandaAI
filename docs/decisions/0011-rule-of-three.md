# 0011 - Spekülatif soyutlamadan kaçınma (Rule of Three)
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Proje v1 aşamasında; ileride gerekebilecek pattern'leri baştan ekleme eğilimi karşısında
bir tasarım ilkesi gerekti.

## Karar
- Basit başla. Pattern somut bir ihtiyaç doğduğunda eklenir; spekülatif soyutlama yasaktır.
- Aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarılmaz.
- Generic repository, outbox pattern gibi yapılar v1'de kullanılmaz.
- Tek implementasyonlu servise interface yazılmaz (ADR 0007).

(docs/architecture.md "Tasarım Felsefesi", docs/conventions.md "Tasarım Kuralı")

## Gerekçe
- Generic repository dışlandı: EF Core'un `DbSet`'i zaten repository görevi görür. Üzerine bir
  katman daha eklemek mevcut bir soyutlamayı ikinci kez sarmalamaktır ve sorgu esnekliğini azaltır.
- Outbox pattern dışlandı: outbox, veritabanı işlemi ile mesaj yayını arasındaki atomikliği
  sağlar. Bu projede message broker yoktur, dolayısıyla çözeceği bir sorun yoktur. Eklenirse
  geriye yalnızca bir tablo ve bir arka plan görevi maliyeti kalır.
- Tek implementasyonlu servise interface yazılmaz: interface'in arkasında bir seçim yoksa o
  interface soyutlama değil, imzaların ikinci bir kopyasıdır ve her değişiklikte iki dosya
  güncellenir.
- Paralel çalışan agent'lar: "bol soyutlama kullan" talimatı verilirse her agent kendi
  soyutlama stilini icat eder ve kod tabanı tutarsızlaşır.

## Sonuçlar
- Kod tabanı küçük ve doğrudan kalır.
- Kısıt: iki kez tekrar eden kod bilinçli olarak kopya halinde kalır.
- Kısıt: ihtiyaç doğduğunda (ör. ikinci servis implementasyonu) refactor maliyeti o an ödenir.
