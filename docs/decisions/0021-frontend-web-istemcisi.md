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
| Konum | Aynı repo: backend `backend/`, web istemcisi `frontend/` (Karar 6) |

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
- Yeni agent: `web-agent` (`.claude/agents/web-agent.md`). Sorumluluk alanı yalnızca `frontend/`;
  `backend/`.e DOKUNMAZ, backend değişikliği gerekirse DURUR ve lidere bildirir.
- Model: Sonnet (ADR 0017: UI kararları yargı gerektirir).
- Kalite kapısı (ADR 0020) `web-agent` için: `npm run build` (TypeScript hatası 0) ve
  `npm run lint` (0 hata). Backend kontrolleri de çalışır (frontend backend'i bozmamalı).
- **Kabul ekranla verilir:** her aşamanın sonunda lider uygulamayı çalıştırır, kabul akışını
  tarayıcıda yürütür ve ekran görüntüleriyle ürün sahibine sunar. Ürün sahibi akış ve görünüm
  üzerinden onay verir; kod kalitesine review agent bakar.

### 5b. Profil (2026-10-10, ürün sahibi geri bildirimi)
- Her kullanıcının bir profil sayfası vardır: bilgileri görür, görünen adı ve saat dilimini düzenler
  (`PUT /api/auth/me`), şifresini değiştirir (`POST /api/auth/change-password`).
- Şifre değişince **bu cihaz dahil** tüm oturumlar kapanır, kullanıcı yeni şifreyle tekrar giriş yapar.
  "Bu cihaz açık kalsın" denendi ve test onu çürüttü: diğer cihazlar revoke edilmiş token'larıyla
  otomatik refresh denediğinde yeniden kullanım tespiti (ADR 0018) yeni oturumu da kapatıyordu.
  Logout kararıyla tutarlı olarak "her yerden çıkış" seçildi.
- Yanlış mevcut şifre 400 döner, 401 değil: istemci 401'de oturumu kapatır.
- E-posta değiştirme yok: yeni adresin doğrulanması gerekir, bildirim aşamasına kaldı.

### 5c. Tasarım dili: Linear tarzı yerleşim (2026-10-10, ürün sahibi geri bildirimi)
- Önce Raycast ilhamlı bir dil denendi (koyu, ince çerçeve, mercan vurgu, üst header). Ürün sahibi
  beğenmedi: üstteki Pano/Takvim menüsü ve renkli vurgu kutuları istenmedi. Referans linear.app oldu.
- **Yerleşim:** üst header yok. Solda 232px sidebar: çalışma alanı satırı (AjandaAI ⌄ menüsü: profil,
  tema, çıkış; arama/⌘K ve "Yeni" ikonları), ana menü (Pano, Takvim), Admin'e özel "Yönetim" bölümü,
  altta kullanıcı satırı. İçerik 8px boşluklu, 1px çerçeveli, 10px köşeli **inset panel** içinde; panelin
  kendi 44px üst barı var (sayfa adı + gri meta, sağda nötr kontroller). `[` sidebar'ı daraltır; <900px
  sidebar drawer olur.
- **Renk:** neredeyse tek renk. Aktif menü öğesi renkle değil zemin tonuyla belli olur. Tek vurgu Linear
  indigo #5E6AD2 (birincil buton, odak, "bugün" rozeti, şimdi çizgisi). Renkli uyarı/bilgi kutuları yok.
  Kategori renkleri yalnızca küçük nokta (pano) ve hafif ton + sol şerit (takvim) olarak.
- **Tipografi:** Inter, taban yaklaşık 13px; büyük sayfa başlıkları yok.
- **Giriş ekranları:** tek sütun, ortada, pazarlama paneli yok.
- ⌘K komut paleti (`@mantine/spotlight`) korunur; yeni işlevler palete de komut olarak eklenir.
- Bu dil sonraki ekranların varsayılanıdır.

### 5d. "Cyber-Executive" görsel dili (2026-10-12, ürün sahibinin şartnamesi) — 5c'nin yerine geçer
- Ürün sahibi Linear/Raycast/Notion Calendar standartlarında ayrıntılı bir şartname verdi. Tailwind + Lucide
  istenmişti; sorulunca Mantine'de kalmayı seçti (çalışan form/tarih/⌘K/drawer bileşenleri bozulmasın).
  Görsel kurallar Mantine + CSS ile birebir; ikonlar Tabler (Lucide ile aynı çizgisel stil).
- Katmanlar: canvas #090b10; sidebar ve üst navbar rgba(24,24,27,.8) + blur(12px); çizgiler rgba(255,255,255,.06).
- 48px üst navbar: solda "Sayfa / dönem" + mini Bugün (bugünkü dönemde gizli), ortada command bar (Ctrl/⌘ K),
  sağda Hafta | Ay | Gün + ‹ ›. Pano'dan Ay/Gün takvime `/calendar?view=...` ile gider. Sidebar korunur.
- Pano 5+2 sığ grid; bugün = parlayan accent rozet.
- Kart (pano ve takvim): kategori renginin %10 tonu, 4px sol kategori çizgisi, inset üst ışık, monospace saat
  (zinc-400), "● Kategori" pill, 6 noktalı tutamak. Pano taslağındaki dolu renkli haplar bununla kalktı.
- Boş gün: kesikli dropzone + silik çizgisel ikon + mikro metin; tıklanınca o günün formu açılır. Emoji yok.

### 5e. "Özel Defter" görsel kimliği (2026-11-04, ürün sahibi geri bildirimi) — 5d'nin yerine geçer
- **Neden:** Ürün sahibi 5d'yi kendi cihazında görünce "buzdolabı gibi, 1990'lardan muhasebe uygulaması,
  hazır şablon; ajanda olduğu anlaşılmıyor" dedi. Hedef: kaliteli, telli, renkli bir defter alan kişi nasıl
  daha özenli yazarsa, uygulama da eşsiz görünmeli ki kullanıcı özen göstersin ve hayatını düzene soktuğuna
  inansın. İşlev ve etkileşimler (sürükle-bırak, ⌘K, formlar) AYNEN kalır; yalnızca görsel katman değişir.
- **Referans:** onaylı taslaklar `docs/design/ozel-defter/*.dc.html` (giriş, pano masaüstü + mobil, takvim,
  aktivite formu, profil, yönetim/entegrasyonlar). Ölçüler, renkler ve metinler buradan birebir alınır.
- **Palet:** yeşil, turkuaz, mor, turuncu. Dolgularda koyu tonlar (#15803D, #0F766E, #7C3AED, #C2410C) →
  TÜM metin beyaz ve okunur kalır; canlı tonlar (#4ADE80, #2DD4BF, #A78BFA, #FB923C) yalnızca parlama, çerçeve,
  ikon ve illüstrasyonda. Zemin #120d22 + köşelerde mor/turkuaz radial ışık; kart zemini #1b1530.
  Ana eylemler (giriş, Yeni aktivite, Kaydet) dört renkli gradyan.
- **Tipografi:** Bricolage Grotesque (arayüz), Caveat (yalnızca kısa el yazısı notlar: "bugün", karşılama),
  JetBrains Mono (saatler).
- **Defter dokusu:** gün kartlarında üstte BEYAZ spiral halkalar ve çizgili sayfa zemini; formda gradyan şerit +
  spiral; profilde defter kapağı. Bugün = gökkuşağı gradyan çerçeve + parlama.
- **Kimlik:** spiralli defter + onay işareti logosu her ekranda; girişte büyük defter illüstrasyonu; boş günlerde
  küçük illüstrasyon + samimi not. Görseller satır içi SVG illüstrasyon (fotoğraf yok: telif, boyut, tema uyumu).
- **İkon:** her kategori ve menü öğesinde Tabler ikonu. Kategori → ikon/renk eşlemesi tek yerde tutulur.
- **Yerleşim:** masaüstünde hep açık sol sidebar (5d'deki üst navbar kalkar; dönem başlığı + gezinme içerikte);
  mobilde alt bar + ortada yüzen gradyan "+" butonu; mobil panoda boş günler tek satırda çip.
- **Hareket:** sayfa geçişlerinde yumuşak belirme/kayma, kart eklenince belirme, sürüklerken hafif eğilme;
  `prefers-reduced-motion`'da kapalı.
- **Yönetim → Entegrasyonlar ve Yol Haritası:** Eklendi / Sırada / Planlandı / Fikir durumlarıyla özellik ve
  MCP/yapay zeka listesi. Liste frontend'de SABİT bir veri dosyasıdır (veritabanı yok); her aşama bitince lider
  `docs/roadmap.md` ile birlikte günceller.

### 5f. "Eskiz Defter" görsel dili (2026-11-09, ürün sahibi geri bildirimi) — 5e'nin GÖRSEL katmanının yerine geçer
- **Neden:** 5e'nin koyu zemin + gradyanları birkaç gün sonra "fazla yoğun" geldi. İstenen: sade, yeni alınmış
  eskiz / method defter, kara kalem hissi. 5e'nin GEREKÇESİ (defter hissi, özen) ve yerleşimi (hep açık sidebar,
  mobil alt bar + ortada "+", Entegrasyonlar sayfası, illüstrasyonlu boş durumlar) AYNEN kalır; değişen görünümdür.
- **Referans:** onaylı taslaklar `docs/design/eskiz/Eskiz-Pano.dc.html` ve `Eskiz-Mobil.dc.html`.
- **Renk (açık tema, nötr gri — sarımtırak DEĞİL):** zemin #f2f1ed + noktalı ızgara (rgba(60,60,58,.2), 22px),
  kart #fcfcfa, sidebar/başlık #e4e3df / #e9e8e4, metin ve çizgi grafit #2b2a27, ikincil #5c5b56 / #6b6a65 /
  #8a8984, kesikli çizgi #bdbcb6. Gradyan, parlama, neon YOK. Ana eylem: grafit dolu buton, açık metin.
  Seçili menü: gri fosforlu kalem çizgisi (rgba(120,120,115,.22)).
- **Kategori rengi:** yalnızca 3px sol çizgi, solgun tonlar — adaçayı #6f8f6a, petrol #5e8a8a, lavanta #7d7aa6,
  terrakota #b07a5a. Kart dolgusu kategori renginde DEĞİL.
- **Tipografi:** Kalam (başlık, gün numarası, el yazısı notlar), Inter (gövde), JetBrains Mono (saat).
- **Defter dokusu:** her gün kartının üst kenarında koyu metal SPİRAL tel halkalar (delikten geçip kenarın üstünden
  kıvrılan; ürün sahibinin en sevdiği öğe), hafif düzensiz kalem çerçeve (1.6px grafit) + 3px ofset gölge,
  bugün = daire içinde tarih, tamamlanan = üstü çizili + ✓, el çizimi alt çizgi başlıklarda.
- **Pano:** hafta sonu kartları hafta içinin yarısı kadar değil, belirgin yükseklikte (taslakta 256px), içerik
  alt alta.
- İkonlar: Tabler, çizgisel, grafit. İllüstrasyonlar tek renk kalem çizimi (5e'nin renkli SVG'leri grafite çevrilir).
- Uygulama tek tema (açık); 5e'deki `forceColorScheme="dark"` açığa döner.

### 6. Klasör ayrımı: `backend/` + `frontend/` (2026-10-10)
```
backend/     AjandaAI.sln, src/, tests/   (.NET)
frontend/    React istemcisi (Vite)
docker/, docker-compose.yml               (iki tarafın ortak altyapısı, kökte)
docs/, .claude/                           (kökte)
```
- İlk planda .NET `src/` + `tests/` kökte kalacak, frontend `web/` olacaktı. Ürün sahibi
  isimlerin hangi tarafın ne olduğunu ilk bakışta söylemesini istedi; .NET kodu `git mv` ile
  `backend/` altına taşındı (geçmiş korunur), frontend `frontend/` altında doğar.
- Kökte `.sln` yoktur: komutlar `dotnet build backend`, `dotnet test backend`,
  `dotnet ef ... --project backend/src/...` biçimindedir (docs/commands.md).
- Agent sınırı klasörle örtüşür: backend agent'ları `backend/`, web-agent `frontend/` içinde çalışır.

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
- Repo.ya Node/npm bağımlılığı gelir (Node 20+). `frontend/node_modules` git'e girmez.
- Frontend kodu ürün sahibi tarafından okunmayacağı için review agent ve ekran kabulü zorunludur.
- Takvim aktiviteleri tarih aralığıyla çeker (`From`/`To`); liste uçları en fazla 100 kayıt döndüğü
  için yoğun bir ayda istemci sayfaları sırayla çeker.
- Production dağıtımı (statik dosyaların nereden sunulacağı, CSP, gerçek CORS origin'i) bu
  ADR'nin kapsamı dışındadır; canlıya çıkış adımında karar verilir.
- Kabul akışlarının otomatik E2E testi (Playwright) MVP'de yok; ekran kabulü elle yapılır.
  Akışlar oturdukça eklenebilir.

## Bilinen kısıtlar (F5 final review, 2026-10-19 — bilinçli kabul)

- **Sekmeler arası refresh yarışı:** İki sekme aynı anda refresh yaparsa reuse tespiti tüm
  oturumları kapatır. Kabul: güvenlik tarafında kalmak tercih edildi.
- **Ağ hatasında oturum kapanır:** Refresh sırasında ağ koparsa kullanıcı giriş ekranına düşer.
  Kabul: bağlantı yokken uygulama zaten kullanılamaz.
- **Pasif kategori "Kategorisiz" görünür:** Kategori pasife alınırsa aktiviteleri renksiz görünür.
  Kabul: kategoriler nadiren pasife alınır.
- **Sürükle-bırak GET+PUT:** Arada yapılan başka bir düzenleme ezilebilir. Çözüm (PATCH
  `/reschedule` veya ETag) ileride.
