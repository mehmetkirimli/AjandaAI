# 0005 - int Id kullanımı (Guid yerine)
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Tüm entity'lerin birincil anahtar tipi belirlenmeliydi.

## Karar
Tüm entity'lerde `int Id` kullanılır (veritabanında identity).

## Gerekçe
- Foreign key ilişkilerinde daha az yer kaplar ve daha hızlıdır.
- Seed data için sabit Id vermek kolaydır (`CategoryConfiguration`'da `HasData` ile
  Id 1-10 arası 10 kategori).
- Guid tahmin edilebilirliği azaltır ancak GÜVENLİK SAĞLAMAZ. Sıralı Id'nin yarattığı IDOR
  riski Guid ile gizlenerek değil, yetkilendirme ile çözülür (her sorguda UserId kontrolü).
  Guid'e güvenmek "security through obscurity" olur.
- v1'de authentication yok; v2'de yetkilendirme eklendiğinde her sorguya sahiplik kontrolü
  konulacak. (docs/database.md "Yetkilendirme Notu — v2")

## Sonuçlar
- Kısa, sıralı, okunabilir id'ler (`/api/activities/6`).
- Kısıt: id'ler tahmin edilebilir; yetkilendirme eklenene kadar API açıktır. Auth eklendiğinde
  her sorguda UserId kontrolü yapılmazsa IDOR açığı oluşur.
- Kısıt: id'ler veritabanı tarafından üretilir; kayıt kaydedilmeden id bilinmez.
- Kısıt: dağıtık sistemde (çoklu yazar) Id üretimi gerekirse sorun çıkarabilir; v1 kapsamında
  böyle bir ihtiyaç yok.
