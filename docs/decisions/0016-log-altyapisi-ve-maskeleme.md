# 0016 - Log altyapısı ve hassas veri maskeleme
**Durum:** Kabul edildi
**Tarih:** 2026-09-26

## Bağlam
Uygulamanın ne yaptığını, ne kadar sürdüğünü ve nerede hata aldığını görmek için
log altyapısı gerekiyordu. Log kayıtlarında kullanıcıyı ayırt edebilmek de gerekliydi
("bu hatayı hangi kullanıcı aldı?").

## Karar
- Serilog + MongoDB (`ajandaai_logs` veritabanı, `logs` koleksiyonu).
- Yapılandırma appsettings'ten okunur (`ReadFrom.Configuration`), kodda sink/seviye hardcode yok.
- Kişisel veri maskelenerek loglanır: ne hiç loglanmaz ne de açık yazılır.
- İki katmanlı maskeleme:
  - `MaskingHelper` (Application/Common/Logging) — elle, esas yöntem.
  - `SensitiveDataDestructuringPolicy` (Api/Logging) — otomatik, güvenlik ağı.
- EF sorguları: Development'ta hepsi Information, Production'da Warning; 500 ms üstü
  sorgular `SlowQueryInterceptor` ile ayrıca "SlowQuery" olarak loglanır.

## Gerekçe
Sink seçimi:
- Dosya reddedildi: okunması ve sorgulanması zor.
- MongoDB seçildi: doküman tabanlı sorgulama kolay (structured logging alanları
  `Properties.UserId` gibi doğrudan sorgulanabilir) ve Elasticsearch'e geçiş doğal olur.
- Yapılandırma config'ten okunduğu için sink değişimi kod değişikliği gerektirmez.

Ayrı veritabanı:
- Log hacmi iş verisinden kat kat fazladır. Aynı veritabanında tutulursa yedekleme,
  migration ve performans birbirine karışır.

Maskeleme:
- Hassas veriyi hiç loglamamak hata ayıklamayı zorlaştırır ("hangi kullanıcı" sorusu
  cevapsız kalır).
- Açık loglamak kişisel veri koruma açısından kabul edilemez.
- Maskeleme ikisinin ortasıdır: kayıt ayırt edilebilir kalır, veri açığa çıkmaz
  (`per*****45@hot****.com`).

İki katman:
- Elle maskeleme esastır çünkü niyet bellidir.
- Destructuring policy güvenlik ağıdır: bir geliştirici `{@Dto}` ile nesne loglar ve
  maskelemeyi unutursa yine korunur. Tek başına güvenilmez: skaler bir değeri
  (`{Email}`) yakalamaz.

EF sorguları:
- Development'ta hepsi: öğrenme ve hata ayıklama için.
- Production'da yalnızca yavaş sorgular: hacim ve performans nedeniyle.

## Sonuçlar
- Yeni bir hassas alan eklendiğinde iki yerde güncelleme gerekir: `MaskingHelper` ve
  destructuring policy.
- Maskelenmiş veri geri çevrilemez. Log üzerinden kullanıcıya ulaşmak için `UserId` kullanılır.
- Mongo container'ı ayakta olmalıdır, yoksa Mongo'ya giden loglar kaybolur. Fallback sink
  tanımlı değildir (Development'ta yalnızca Console sink'i ayrıca yazar).
- Kurallar: [docs/conventions.md](../conventions.md) "Log Kuralları".
