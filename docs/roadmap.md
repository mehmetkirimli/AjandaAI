# Yol Haritası

Ürünün nerede olduğu ve sıradaki işler. Kararların NEDEN'i ilgili ADR'dedir; bu dosya yalnızca
sırayı ve durumu tutar. Her aşama bitince güncellenir.

Son güncelleme: 2026-10-10

## Genel durum

| Aşama | Durum | Karar |
|---|---|---|
| v1: CRUD, sayfalama, log altyapısı | ✅ Bitti | ADR 0001–0016 |
| v2.1: Authentication | ✅ Bitti | ADR 0018, 0019, 0020 |
| **v2.2: Frontend (web)** | ⏳ **Sıradaki** | ADR 0021 |
| v2.3: MCP entegrasyonu | Planlandı | ADR yazılacak |
| v2.4: Bildirim altyapısı (SMTP, hatırlatma gönderimi, temizlik işleri) | Ertelendi | — |
| v2.5: Davet sistemi / ortak aktivite | Ertelendi | — |
| Sonra: analiz ajanı (Rating, AiNote), plan gruplama, koordinat/harita | Fikir | docs/domain.md |

Sıralama kararı (2026-10-09): ürün önce görünür olsun (frontend), sonra "havalı" entegrasyonlar
(MCP). Bildirim canlıya çıkıştan önce şarttır (Production'da register `IEmailSender` bekliyor),
ama geliştirmenin önünde engel değildir.

## v2.2 Frontend — adımlar (ADR 0021)

Kritik yol: F0 → F1 → F2 → F3 → F4 → F5. Sıralı çalışılır (ADR 0020).

| Adım | Kim | İş | Kabul (ekranda görülen) |
|---|---|---|---|
| **F0** Backend hazırlık ✅ (2026-10-10) | Lider | CORS, `Frontend:BaseUrl` + doğrulama linki, `GET /api/auth/me`, Swagger Bearer, `web-agent` tanımı, kalite kapısına web kontrolleri | Swagger'da "Authorize" ile korumalı uç denenir; log'daki doğrulama linki frontend adresini gösterir |
| **F1** İskelet + giriş + Haftalık Pano ✅ (2026-10-10, [ekranlar](screenshots/f1/)) | web-agent | `frontend/` (Vite + React + TS + Mantine), yönlendirme, sürükle-bırak Haftalık Pano (@dnd-kit; kartı başka güne taşı, saat korunur), API istemcisi (bellekte access, localStorage'da refresh, tek refresh kilidi), ana düzen (üst menü), kayıt / doğrulama / giriş / çıkış sayfaları | Kayıt ol → log'daki linke tıkla → doğrulandı → giriş → adın üst menüde → panoda bir kartı başka güne sürükle, yenileyince orada kalır → sayfa yenilenince oturum kalır → çıkış |
| **F2** Takvim | web-agent | FullCalendar hafta/ay görünümü, tarih aralığına göre aktiviteler, kategori renkleri, tıklayınca detay paneli, sürükle/uzat ile saat güncelleme | Swagger'dan eklenen aktiviteler takvimde görünür; sürüklenen aktivite yenilemeden sonra yeni saatinde kalır |
| **F3** Aktivite + hatırlatma | web-agent | Ekle/düzenle/pasife al formu, alan bazında hata mesajları, detayda hatırlatma listesi / ekle / sil | Takvimde boş bir saate tıkla → form → kaydet → takvimde belirir; hatalı girişte alanın altında Türkçe mesaj |
| **F4** Admin | web-agent | Yalnızca Admin'de görünen menü: kullanıcılar (liste, ekle, rol, pasife al), kategoriler, aktivite üstverisi + moderasyon | User rolünde admin menüsü yok; Admin rolünde kullanıcıyı pasife alınca listede pasif görünür |
| **F5** Cila + review + demo | web-agent + review agent | Boş durumlar, yükleniyor/hata ekranları, küçük ekran uyumu; final review; demo turu | Ekran görüntüleriyle uçtan uca demo |

## v2.3 MCP — taslak (ADR yazılacak)

Hedef: Claude (Desktop / Code) doğal dille ajandayı yönetsin; frontend'deki takvimde anında görünsün.

Açık sorular (ADR'de cevaplanacak):
- Makine istemcisinin kimliği: kişisel erişim token'ı mı, OAuth mu? (ADR 0018 makine istemcilerini
  öngörmüştü ama login akışı tarayıcı içindir.)
- Hangi araçlar (tools): aktivite listele/ekle/güncelle, hatırlatma ekle, haftalık özet...
- MCP sunucusu nerede çalışır: ayrı bir proje mi (`backend/src/AjandaAI.Mcp`), API içinde mi?
- Dış connector'lar (hava durumu, harita, Google Calendar) bu aşamada mı, sonra mı?

## Ertelenenlerin notları
- **Bildirim:** `SmtpEmailSender`, hatırlatma gönderen arka plan servisi, doğrulanmamış kayıt ve
  biriken refresh token temizliği (ADR 0018).
- **Canlıya çıkış öncesi:** rate limit + reverse proxy ayarı, CSP, gerçek CORS origin'i, statik
  dosya sunumu (ADR 0018 "Bilinen kısıtlar", ADR 0021 "Sonuçlar").
