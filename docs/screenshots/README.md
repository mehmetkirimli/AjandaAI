# Ekran kabulleri

Frontend adımlarının kabulü ekranla verilir (ADR 0021). Her adımın klasöründe lider tarafından
gerçek tarayıcıda (Playwright + yerel Chrome) alınan ekran görüntüleri ve kullanılan senaryo durur.

- [f1/](f1/) — kayıt → doğrulama → giriş → Haftalık Pano → sürükle-bırak → yenileme → koyu tema → çıkış
  (2026-10-10). Senaryo: `f1/kabul-senaryosu.mjs` (çalıştırmak için ayrı bir klasörde
  `npm i playwright`; backend ve `npm run dev` açık olmalı; `API_LOG` ve `OUT` ortam değişkenleri).
- [f1-profil/](f1-profil/) — kompakt tasarım + profil: bilgi düzenleme, yanlış mevcut şifre, şifre değiştirme → tekrar giriş
  (2026-10-10, 1536x864 ≈ 1920 px ekran %125 ölçek). Senaryo: `f1-profil/kabul-senaryosu.mjs` (`API`, `WEB` ortam değişkenleri).
- [f2/](f2/) — takvim: hafta, sürükle (Çar 08:00 → Per 10:00), süre uzat, detay paneli, ay, koyu tema, Pano senkronu (2026-10-10). Senaryo: `f2/kabul-senaryosu.mjs`.
- [tasarim/](tasarim/) — (REDDEDİLDİ, yerine linear/) Raycast ilhamlı tasarım dili (koyu öncelikli) ve Ctrl+K komut paleti; tüm ekranlar, açık tema, davranış regresyon kontrolü (2026-10-10).
- [linear/](linear/) — Linear tarzı yerleşim (geçerli tasarım): sol sidebar, inset panel, giriş, pano ve takvim sürükle-bırak, çalışma alanı menüsü, ⌘K, sidebar daraltma, profil, açık tema, dar ekran (2026-10-10).
- [pano-taslak/](pano-taslak/) — ürün sahibinin Pano taslağı: hafta içi/hafta sonu iki sıra, dolu renkli haplar, boş gün mesajları, + Aktivite Ekle; boş güne ve hafta sonuna sürükle-bırak (2026-10-12).
- [f3/](f3/) — aktivite formu: Pano'dan ekle, C kısayolu + doğrulama, takvimde aralık seçip oluştur, Düzenle, hatırlatma ekle/sil, pasife al (2026-10-12).
- [f3b/](f3b/) — tekrarlayan seri: krem (her gün, 12 Eki → 12 Nis, 183, 15 dk hatırlatma), her Pazartesi iş (uzun süre uyarısı → Tekrarla), seri ikonu, "Bu ve sonrakileri durdur" (2026-10-12).
- [cyber/](cyber/) — GEÇERLİ TASARIM: "Cyber-Executive" (48px navbar + command bar, 5+2 sığ grid, yarı saydam kart + sol çizgi + mono saat, kesikli dropzone); pano/takvim sürükle, dropzone→form, navbar Ay, ⌘K tema (2026-10-12).
