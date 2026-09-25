# 0002 - Enum'ların veritabanında string olarak saklanması
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Domain'de `ActivityStatus`, `Priority`, `EnergyLevel` enum'ları var. EF Core varsayılan
olarak enum'ları int olarak saklar.

## Karar
Tüm enum'lar veritabanında STRING olarak saklanır; configuration'da
`HasConversion<string>()` kullanılır. Int saklama yasaktır. (docs/database.md, docs/domain.md)

## Gerekçe
- Int saklamada enum'un ortasındaki bir konuma yeni değer eklenirse mevcut kayıtların
  anlamı kayar. String saklamada böyle bir risk yoktur.
- Veritabanına doğrudan bakarken (DBeaver) `Completed` okunabilir, `2` okunabilir değildir.
- Kod tarafında hâlâ tip güvenli enum kullanılır; yalnızca saklama formatı değişir.

## Sonuçlar
- Veritabanındaki değerler enum adıyla birebir aynıdır (`Planned`, `High` ...).
- Kısıt: bir enum üyesinin ADI değiştirilirse mevcut kayıtlar bozulur; isim değişikliği
  veri migration'ı gerektirir.
- Kısıt: string kolon int'e göre biraz daha fazla disk alanı kullanır (ihmal edilebilir).
