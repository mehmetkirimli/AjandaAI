# 0004 - DateTimeOffset kullanımı (DateTime yerine)
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Aktivite (Start, End), hatırlatıcı (RemindAt, SentAt) ve audit alanları (CreatedAt, UpdatedAt)
zaman bilgisi taşır. Kullanıcıların farklı saat dilimleri olabilir (`User.TimeZoneId`).

## Karar
Tüm zaman alanları `DateTimeOffset` olarak tutulur; sunucu zamanı `DateTimeOffset.UtcNow`
ile üretilir.

## Gerekçe
- `DateTimeOffset`, C# tarafında offset bilgisini taşır. Npgsql veritabanına yazarken değeri
  UTC'ye çevirir ve `timestamptz` olarak saklar; okurken `+00:00` offset ile döner. Yani
  veritabanında saklanan şey mutlak andır, orijinal saat dilimi değildir.
- Faydası yazma tarafındadır: istemci `2026-09-24T14:00:00+03:00` gönderdiğinde sunucu hangi
  anı kastettiğini kesin bilir. `DateTime` kullanılsaydı aynı değer, sunucunun ve container'ın
  saat dilimine göre farklı yorumlanırdı.
- Docker UTC, geliştirme makinesi UTC+3 ile çalıştığı için bu belirsizlik gerçek bir sorundur.
- Kullanıcının kendi saat dilimi ayrıca `User.TimeZoneId` alanında tutulur; "kullanıcı için
  saat kaç" sorusu bu alanla cevaplanır.

## Sonuçlar
- Değerler mutlak bir zaman noktasını temsil eder.
- Kısıt: orijinal offset veritabanında saklanmaz; okunan değerler her zaman `+00:00` offset
  ile gelir. Yerel saat gösterimi `User.TimeZoneId` ile yapılmalıdır.
- Kısıt: yalnızca tarih tutan alanlar için de offset taşınır (gereksiz ama zararsız).
