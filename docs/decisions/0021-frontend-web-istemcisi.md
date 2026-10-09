# 0021 - Frontend (web istemcisi)
**Durum:** Kabul edildi (uygulama henüz yapılmadı)
**Tarih:** 2026-10-09

## Bağlam
v2 Authentication tamamlandı (ADR 0018, 0019); backend'in tüm uçları token'la korunuyor ama
henüz hiçbir gerçek istemci yok. ADR 0018 istemci sırasını belirlemişti: önce tarayıcıda çalışan
web uygulaması, sonra mobil, ayrıca makine istemcileri (MCP). Framework "Angular/React, henüz
seçilmedi" olarak bırakılmıştı.

Ürün sahibi frontend bilmiyor; kodu agent'lar yazacak, kabul ekranda görülen davranışla verilecek.
Hedef, ürünün "boy göstermesi": şu ana kadar yapılan auth ve CRUD işinin bir arayüzle görünmesi.

Sıralama kararı (ürün sahibi, 2026-10-09): önce frontend, sonra MCP. Bildirim altyapısı ve davet
sistemi sonraki aşamaya ertelendi (bkz. docs/roadmap.md).

## Karar

### 1. Teknoloji
| Parça | Seçim |
|---|---|
| Dil / çatı | TypeScript + React, Vite ile |
| Bileşen kütüphanesi | Mantine (form, tablo, tarih seçici, modal, bildirim) |
| Takvim | FullCalendar (React), haftalık + aylık görünüm, sürükle-bırak |
| Sunucu durumu / API | TanStack Query + tek bir `fetch` tabanlı API istemcisi |
| Yönlendirme | React Router |
| Konum | Aynı repo, `web/` klasörü (`src/` .NET'e ait kalır) |

Mobil istemci geldiğinde React Native (Expo) ile yazılır; tipler ve API istemcisi paylaşılabilir.

### 2. Token saklama (ADR 0018 "Bilinen risk"in somutlaşması)
- Access token yalnızca **bellekte** tutulur (sayfa yenilenince refresh ile yeniden alınır).
- Refresh token `localStorage`'da tutulur. XSS ile çalınabilir; azaltan önlemler: rotation +
  yeniden kullanım tespiti (backend'de var), frontend'de `dangerouslySetInnerHTML` yasağı,
  canlıya çıkışta CSP. Geri dönüş yolu ADR 0018'deki gibi: web için HttpOnly cookie eklemek.
- 401 alınınca API istemcisi **bir kez** refresh dener; aynı anda gelen 401'ler tek bir refresh
  isteğini bekler (rotation'da çifte refresh tüm oturumları kapatır, ADR 0018). Refresh de 401
  dönerse oturum kapanır, kullanıcı login sayfasına gider.

### 3. MVP ekranları
1. Kayıt, e-posta doğrulama (`/verify-email?token=...`), giriş, çıkış.
2. **Takvim** (ana ekran): aktiviteler haftalık/aylık; kategoriye göre renk; tıklayınca detay;
   sürükle/uzat ile Start/End güncellenir.
3. Aktivite ekle/düzenle/pasife al formu (kategori, öncelik, enerji, konum, bütçe, esneklik).
4. Aktivite detayında hatırlatma listesi, ekleme ve silme.
5. Admin sayfası (yalnızca Admin rolünde görünür): kullanıcılar (listele, ekle, rol değiştir,
   pasife al), kategoriler, aktivite üstverisi ve moderasyon.

Arayüz dili Türkçe. Backend'in hata mesajları (zaten Türkçe) form alanlarının altında gösterilir.

### 4. Backend'de gereken ekler (F0)
- CORS: Development'ta `http://localhost:5173` (Vite) izinli; izinli origin'ler config'den okunur.
- `Frontend:BaseUrl` ayarı: doğrulama e-postasındaki link frontend sayfasını gösterir
  (`{BaseUrl}/verify-email?token=...`). `LogEmailSender` bu linki log'a yazar.
- `GET /api/auth/me`: oturumdaki kullanıcının Id, e-posta, görünen ad, saat dilimi ve rolü
  (üst menü ve admin menüsünün görünürlüğü için).
- Swagger'a Bearer tanımı (review bulgusu, ADR 0018 "Bilinen kısıtlar").

### 5. Agent ve kalite kapısı
- Yeni agent: `web-agent` (`.claude/agents/web-agent.md`). Sorumluluk alanı yalnızca `web/`;
  `src/` ve `tests/`'e DOKUNMAZ, backend değişikliği gerekirse DURUR ve lidere bildirir.
- Model: Sonnet (ADR 0017: UI kararları yargı gerektirir).
- Kalite kapısı (ADR 0020) `web-agent` için: `npm run build` (TypeScript hatası 0) ve
  `npm run lint` (0 hata). Backend kontrolleri de çalışır (frontend backend'i bozmamalı).
- **Kabul ekranla verilir:** her aşamanın sonunda lider uygulamayı çalıştırır, kabul akışını
  tarayıcıda yürütür ve ekran görüntüleriyle ürün sahibine sunar. Ürün sahibi akış ve görünüm
  üzerinden onay verir; kod kalitesine review agent bakar.

## Gerekçe
- **React:** En yaygın frontend çatısı; agent'ların en tutarlı kod ürettiği ekosistem. Mobil için
  React Native aynı zihinsel modeli kullanır; Angular'da mobil yolu (Ionic/NativeScript) daha zayıf.
- **Mantine:** CSS bilmeden tutarlı ve şık görünüm; tarih/saat seçici ve form yönetimi hazır.
  Tailwind + shadcn gibi seçenekler daha esnek ama her ekranda tasarım kararı gerektirir.
- **FullCalendar:** Ajanda uygulamasının vitrini takvimdir; sürükle-bırak ve hafta/ay görünümü
  hazır gelir. Kendi takvimini yazmak MVP için gereksiz maliyet.
- **TanStack Query:** Yükleniyor/hata/önbellek/yeniden deneme durumlarını tek desenle çözer;
  token yenilemeyi tek bir API istemcisinde toplamayı kolaylaştırır.
- **Aynı repo:** Backend ve frontend değişikliği aynı commit'te görünür; agent'lar API
  sözleşmesini (DTO'lar) doğrudan okuyabilir.
- **Access token bellekte:** localStorage'daki bir access token XSS'te anında kullanılır; bellekte
  tutmak saldırı yüzeyini refresh token'a indirir, onu da rotation korur.
- **Tek refresh kilidi:** Rotation + yeniden kullanım tespiti, aynı refresh token'ın iki kez
  gönderilmesini hırsızlık sayar ve tüm oturumları kapatır. Paralel 401'lerde iki refresh isteği
  atan bir istemci kullanıcıyı kendi kendine dışarı atar.

## Sonuçlar
- Repo'ya Node/npm bağımlılığı gelir (Node 20+). `web/node_modules` git'e girmez.
- Frontend kodu ürün sahibi tarafından okunmayacağı için review agent ve ekran kabulü zorunludur.
- Takvim aktiviteleri tarih aralığıyla çeker (`From`/`To`); liste uçları en fazla 100 kayıt döndüğü
  için yoğun bir ayda istemci sayfaları sırayla çeker.
- Production dağıtımı (statik dosyaların nereden sunulacağı, CSP, gerçek CORS origin'i) bu
  ADR'nin kapsamı dışındadır; canlıya çıkış adımında karar verilir.
- Kabul akışlarının otomatik E2E testi (Playwright) MVP'de yok; ekran kabulü elle yapılır.
  Akışlar oturdukça eklenebilir.
