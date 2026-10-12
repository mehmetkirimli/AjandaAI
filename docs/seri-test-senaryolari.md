# Seri (tekrarlayan aktivite) test senaryoları

Kararlar: [ADR 0022](decisions/0022-tekrarlayan-aktivite-serisi.md). Kimlik test adında veya yorumunda geçer
(`SERIES-xx`); kalite kapısı (ADR 0020) bu kimlikleri arar.

## Oluşturma
| Id | Senaryo | Beklenen |
|---|---|---|
| SERIES-01 | Daily, 12 Eki 21:00–21:10 → 12 Nis | 183 aktivite, hepsi aynı `SeriesId`, her biri yerel 21:00–21:10 |
| SERIES-02 | Weekdays, 1 hafta | yalnızca Pzt–Cum (5) |
| SERIES-03 | Weekends, 2 hafta | yalnızca Cmt–Paz (4) |
| SERIES-04 | Weekly [Pzt, Çar, Cum], 2 hafta | 6 aktivite, yalnızca o günler |
| SERIES-05 | Weekly, `daysOfWeek` boş | 400; hiçbir şey oluşmaz |
| SERIES-06 | Monthly, başlangıç 31 Ocak, 4 ay | 31 Oca, 28 Şub (yoksa son gün), 31 Mar, 30 Nis |
| SERIES-07 | Yaz saati geçişi olan saat diliminde (Europe/Berlin) Daily 21:00, Mart sonunu kapsar | her oluşum yerel 21:00 |
| SERIES-08 | 367 oluşum üretecek kural | 400 ("en fazla 366"); hiçbir şey oluşmaz |
| SERIES-09 | `until` ilk günden önce | 400 |
| SERIES-10 | Şablonda geçersiz alan (boş başlık / başkasının veya pasif kategori) | 400, aynı Türkçe mesajlar; hiçbir şey oluşmaz |
| SERIES-11 | `reminderMinutesBefore = 15` | her gelecek oluşumda 1 hatırlatma (Start − 15 dk); geçmişe düşenlerde yok |
| SERIES-12 | Yanıt | `seriesId`, `createdCount`, `firstStart`, `lastStart` doğru |
| SERIES-13 | Body'de başka `UserId` | yok sayılır; sahip token'daki kullanıcı (AUTH-47 ile aynı ilke) |

## Seri işlemleri ve sahiplik
| Id | Senaryo | Beklenen |
|---|---|---|
| SERIES-20 | Detay (`GET /api/activities/{id}`) seriye ait aktivitede | `series` özeti döner; seri dışı aktivitede `series = null` |
| SERIES-21 | `deactivate?from=` ortadaki bir gün | o gün ve sonrası pasif, öncekiler aktif; yanıtta etkilenen sayı |
| SERIES-22 | Seriden tek bir aktiviteyi sürükleyip/düzenlemek | yalnızca o aktivite değişir, `SeriesId` korunur |
| SERIES-23 | Kullanıcı A, B'nin serisine `deactivate` | 404; B'nin aktiviteleri değişmez |
| SERIES-24 | Token'sız `POST /api/activities/series` | 401 (AUTH-48 kapsamında da) |
