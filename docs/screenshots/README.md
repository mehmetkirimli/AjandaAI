# Ekran kabulleri

Frontend adımlarının kabulü ekranla verilir (ADR 0021). Her adımın klasöründe lider tarafından
gerçek tarayıcıda (Playwright + yerel Chrome) alınan ekran görüntüleri ve kullanılan senaryo durur.

- [f1/](f1/) — kayıt → doğrulama → giriş → Haftalık Pano → sürükle-bırak → yenileme → koyu tema → çıkış
  (2026-10-10). Senaryo: `f1/kabul-senaryosu.mjs` (çalıştırmak için ayrı bir klasörde
  `npm i playwright`; backend ve `npm run dev` açık olmalı; `API_LOG` ve `OUT` ortam değişkenleri).
