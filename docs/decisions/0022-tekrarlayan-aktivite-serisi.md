# 0022 - Tekrarlayan aktiviteler: seri olarak toplu oluşturma
**Durum:** Kabul edildi (uygulandı: 2026-10-12)
**Tarih:** 2026-10-12

## Bağlam
Ürün sahibi tekrarlayan işleri tek tek eklemek istemiyor. Örnek: "12 Ekim – 12 Nisan arası, 6 ay
boyunca her akşam 21:00'de krem süreceğim, bana hatırlat." Bu yaklaşık 180 aktivite demek.
İki yol değerlendirildi:
- **A) Seri olarak toplu oluşturma:** tek istekle N ayrı aktivite oluşturulur, hepsi bir seriye bağlanır.
- **B) Gerçek tekrarlama kuralı (RRULE):** tek kayıt + kural saklanır, günler okurken hesaplanır.

## Karar
**A seçildi** (ürün sahibi kararı). Tekrar seçeneklerinin hepsi desteklenir: aktiviteden aktiviteye değişir.

### 1. Veri modeli
- Yeni tablo `activity_series`: `Id`, `UserId` (FK users), `Frequency`, `DaysOfWeek` (bit maskesi, Pzt=1 … Paz=64),
  `StartDate`, `EndDate` (tarih, kullanıcının yerel günü), `CreatedAt`.
- `Frequency` (string, ADR 0002): `Daily` (her gün), `Weekdays` (Pzt–Cum), `Weekends` (Cmt–Paz),
  `Weekly` (seçilen günler; `DaysOfWeek` zorunlu), `Monthly` (her ay `StartDate`'in gün numarası).
- `activities.SeriesId` (int?, FK `activity_series`, index). Seriye ait olmayan aktivitelerde null.
- Seri silinmez (soft delete ADR 0006 uyarınca aktiviteler pasife alınır); bu yüzden FK `Restrict`.

### 2. Oluşturma: `POST /api/activities/series`
- Gövde: aktivite şablonu (`ActivityCreateDto` alanları; `Start`/`End` İLK oluşumun zamanı) +
  `recurrence { frequency, daysOfWeek[], until }` + isteğe bağlı `reminderMinutesBefore` (her oluşuma
  aynı göreli hatırlatma).
- Oluşumlar **kullanıcının saat diliminde** (`User.TimeZoneId`) üretilir: "her akşam 21:00" yaz/kış saati
  geçişinde de yerel 21:00 kalır; süre (End − Start) her oluşumda korunur.
- `Monthly`: ay o güne sahip değilse (ör. 31) **ayın son günü** kullanılır.
- Sınırlar: en az 1, en fazla **366** oluşum; `until` ≥ ilk oluşumun günü.
- Hepsi **tek transaction**'da oluşturulur: biri bile doğrulamadan geçmezse hiçbiri oluşmaz.
- Hatırlatma, zamanı geçmişe düşen oluşumlarda atlanır (hata değil); diğerlerine eklenir.
- Doğrulama tek aktivite oluşturmayla aynıdır (başlık, kategori, bitiş > başlangıç, bütçe, ...).
- Yanıt: `{ seriesId, createdCount, firstStart, lastStart }`.

### 3. Seri işlemleri
- Her oluşum bağımsız bir aktivitedir: tek tek sürüklenir, düzenlenir, pasife alınır (bugünkü gibi).
- `POST /api/activities/series/{seriesId}/deactivate?from={tarih}`: serinin `from` gününden itibaren
  (dahil) olan tüm oluşumlarını pasife alır ("bu ve sonrakileri durdur"). Yanıt: etkilenen sayı.
- `GET /api/activities/{id}` detayına `series` eklenir: `{ id, frequency, daysOfWeek[], startDate, endDate }`.
- Sahiplik ADR 0018 Karar 3 ile aynı: seri işlemleri `WHERE user_id = @me`; başkasının serisi 404.

### 4. Arayüz
- Aktivite formunda **"Tekrarla"** bölümü (sıklık seçici, haftalık için gün çipleri, bitiş tarihi,
  canlı önizleme: "Her gün · 12 Eki → 12 Nis · 183 aktivite"). Ayrıca sidebar ve ⌘K'da
  **"Toplu aktivite ekle"** girişi formu Tekrarla bölümü açık başlatır.
- Hatırlatma formda seçilir (yok / 15 dk / 1 sa / 1 gün önce).
- Detay panelinde seri bilgisi ("Seri · Her gün · 12 Eki → 12 Nis") ve
  **"Bu ve sonrakileri durdur"** butonu (onaylı).

## Gerekçe
- **A, B'ye göre:** Mevcut her şey (pano, takvim, sürükle-bırak, hatırlatmalar, sahiplik sorguları) tek
  aktivite üzerine kurulu; A hiçbirini değiştirmeden çalışır. B'de "yalnızca bu günü değiştir" istisnaları,
  sanal oluşumlara hatırlatma, sürüklemede "bu mu tümü mü?" ve sayfalama yeniden tasarlanmak zorundaydı.
- **Seri tablosu (yalnızca SeriesId değil):** "Bu ve sonrakileri durdur" ve detaydaki özet için kuralın
  kendisi saklanmalı; ileride "seriyi uzat" da buna dayanır.
- **366 sınırı:** Günlük bir yıl yeter; sınırsız istek tek seferde on binlerce satır üretip tabloyu
  şişirebilirdi (kötü niyet veya yanlış giriş).
- **Kullanıcı saat dilimi:** UTC'de üretmek yaz saati geçişinde akşam 21:00'i 20:00'ye kaydırırdı.
- **Tek transaction:** Yarım kalmış bir seri (90 tane oluştu, gerisi hata) kullanıcıyı şaşırtır.

## Sonuçlar
- 6 aylık günlük seri ~180 satır + ~180 hatırlatma üretir; mevcut ölçekte sorun değil.
- Seri kuralını değiştirmek (ör. saati 21:00 → 22:00) bu sürümde yok: kullanıcı "bu ve sonrakileri durdur"
  ile eski seriyi bitirip yenisini oluşturur.
- Bitiş tarihsiz ("sonsuza kadar") seri yok (B'nin avantajıydı); en fazla 366 oluşum.
- Kabul testleri: [docs/seri-test-senaryolari.md](../seri-test-senaryolari.md).
