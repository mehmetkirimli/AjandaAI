# 0024 - MCP sunucusu ve kişisel erişim anahtarı
**Durum:** Kabul edildi (2026-11-09; test senaryoları yazılacak)
**Tarih:** 2026-11-09

## Bağlam
v2.3'ün hedefi: Claude (Code / Desktop) doğal dille ajandayı yönetsin, değişiklik takvimde anında
görünsün. Bunun için AjandaAI'nin Claude'a araç (tool) açan bir MCP sunucusu olması gerekir.
İki engel var:
- **Kimlik:** Mevcut giriş akışı (ADR 0018) tarayıcı içindir; Claude giriş formu dolduramaz.
- **Kapsam:** Ürün sahibi hava durumu, harita, log analizi, WhatsApp, hafıza gibi birçok MCP fikri getirdi.
  Bunların bir kısmı bizim yazacağımız kod, bir kısmı Claude'a takılan hazır sunucular.

## Karar

### 1. Kimlik: kişisel erişim anahtarı (ürün sahibi kararı)
OAuth şimdilik YOK (ileride, uygulama herkese açılırsa; anahtar yöntemiyle yan yana çalışır, bozmaz).
- Kullanıcı profil sayfasından anahtar üretir; anahtar **yalnızca bir kez** gösterilir.
- Biçim: `ajd_` + 32 bayt rastgele (base64url). Veritabanında yalnızca **SHA-256 özeti** saklanır
  (refresh token ile aynı yaklaşım, ADR 0018); ekranda tanımak için ilk 8 karakter (`prefix`) tutulur.
- Yeni tablo `personal_access_tokens`: `Id`, `UserId` (FK), `Name`, `TokenHash` (unique), `Prefix`,
  `CreatedAt`, `ExpiresAt` (varsayılan 90 gün), `LastUsedAt`, `RevokedAt`.
- Kullanıcı başına en fazla **5** etkin anahtar.
- Uçlar (yalnızca normal JWT oturumuyla; bir anahtar başka anahtar ÜRETEMEZ):
  `GET /api/auth/tokens`, `POST /api/auth/tokens`, `DELETE /api/auth/tokens/{id}` (iptal).
- Ayrı bir kimlik doğrulama şeması `PersonalAccessToken`; **yalnızca `/mcp` yolunda** kabul edilir.
  Anahtarla `/api/...` uçlarına girilemez (sızıntıda etki alanı dar kalır).
- Pasif kullanıcı, süresi dolmuş veya iptal edilmiş anahtar → 401. Şifre değişince anahtarlar KALIR
  (bilinçli: Claude bağlantısı kopmasın; iptal ayrı düğme).
- Anahtar loglanmaz; maskeleme listesine eklenir (ADR 0016). Rate limit `/mcp` için de geçerli.

### 2. MCP sunucusu: API'nin içinde
- Resmi .NET MCP SDK'sı (`ModelContextProtocol.AspNetCore`), Streamable HTTP, uç: `/mcp`.
- Araçlar Application servislerini çağırır: doğrulama, sahiplik (`ICurrentUser`, ADR 0019) ve loglama
  web istemcisiyle AYNI yoldan geçer. Araç kodu iş kuralı içermez.
- İlk araçlar (ürün sahibi onayı):

| Araç | Ne yapar |
|---|---|
| `aktivite_ekle` | Başlık, başlangıç/bitiş, kategori (+ isteğe bağlı açıklama/konum) ile ekler |
| `haftayi_listele` | Verilen tarihin haftasındaki aktiviteler (kullanıcının saat diliminde) |
| `bos_zaman_bul` | Tarih aralığı + en az süre (+ gün içi pencere, varsayılan 08:00–22:00) → boşluklar |
| `kategorileri_listele` | Claude'un doğru kategoriyi seçebilmesi için |
| `seri_ekle` | Tekrarlayan seri (ADR 0022, en fazla 366 oluşum, tek transaction) — "1 yıl her gece kitap oku" tek çağrıda |

- Silme ve güncelleme araçları ilk turda YOK: Claude'un elinde geri alınamaz yetki olmasın.
- **İstek sınırı:** `/mcp` için anahtar başına **dakikada 30 araç çağrısı** (aşılırsa 429). Toplu ekleme
  ihtiyacı sınırı gevşeterek değil `seri_ekle` ile karşılanır; sınır, kontrolden çıkan bir istemciye karşı
  emniyet kemeridir (ürün sahibi kararı, 2026-11-09).

### 2b. Geçmiş tarih kuralı — HER YERDE (ürün sahibi kararı, 2026-11-09)
- Bir aktivitenin başlangıcı **en fazla 30 gün geçmişte** olabilir. Gerekçe: kullanıcı yaptığı bir işi sonradan
  ekleyip puan/not verebilsin, ama 30 gün içinde ki ne yaptığını hatırlasın.
- Kural iş katmanındadır (Application doğrulaması): web formu, takvimde sürükleme, seri ve Claude araçları
  aynı kuralı paylaşır.
- Oluşturmada her zaman; güncellemede **yalnızca başlangıç değişiyorsa** uygulanır (eski bir aktivitenin
  puanını/notunu düzenlemek engellenmez).
- Seri: ilk oluşum 30 günden eski olamaz. Mevcut eski kayıtlara dokunulmaz.
- Zamanlar ISO 8601 + offset alınır/verilir (ADR 0004); "gün" sınırları kullanıcının saat dilimidir.

### 3. Claude'a takılan hazır MCP'ler (kod değil, ayar) — v2.3b
Claude bu sunucuları bizim araçlarla kendisi birleştirir ("Cumartesi hava güzelse piknik ekle").
- **MongoDB (log analizi):** resmi MongoDB MCP; **salt okunur** ayrı veritabanı kullanıcısıyla.
  Küçük ölçekte Elastic yerine yeterli.
- **Hava durumu, harita:** hazır sunucular; seçim v2.3b'de yapılır.
- Kurulum adımları `docs/mcp-kurulum.md`'de tutulur.

### 4. Kapsam dışı (bilinçli)
- OAuth, silme/güncelleme araçları.
- Anahtar yeni bir cihazdan ilk kez kullanılınca bildirim: e-posta altyapısıyla birlikte (v2.4).
- WhatsApp: hazır sunucuların çoğu WhatsApp Web'i taklit eder (kurallara aykırı, numara banlanma riski);
  resmi yol Business API (ücretli, onaylı). En sona.
- Ürün içinde hava durumu / harita gösterimi (kartta ikon vb.): ayrı aşama.

### 5. Hafıza (memory) MCP'leri (ürün sahibi kararı, 2026-11-09)
- **`server-memory`: EVET, ürün tarafında** — Claude Desktop'ta ajanda asistanı olarak kullanıcının planlama
  tercihlerini ("sporu sabah yaparım", "Pazar boş kalsın") hatırlar ve `bos_zaman_bul` ile birleştirir.
  Ayar işidir, v2.3b'de.
- **Geliştirme tarafında hafıza MCP'si YOK:** CLAUDE.md + ADR/yol haritası + lider hafızası zaten var;
  dördüncü kaynak "iki doğru kaynak" sorunu yaratır.
- **Codebase memory: şimdilik YOK.** Kod tabanı küçük, arama yeterli; agent turlarında harita eskir ve
  yanlış bilgiyi güvenle verir. Kod tabanı çok büyürse yeniden değerlendirilir.

## Gerekçe
- Anahtar yöntemi birkaç günlük iş, OAuth v2.1 auth turu kadar büyük. Tek kullanıcılı geliştirme
  aşamasında risk/fayda anahtardan yana; anahtarın yalnızca `/mcp`'de geçmesi sızıntı etkisini sınırlar.
- API içinde olması: ayrı süreç, ayrı yapılandırma, ayrı kimlik yok; servisler doğrudan kullanılır.

## Sonuçlar
- İş sırası (ADR 0020, sıralı): db-agent (tablo + migration) ✅ → catalog-agent (anahtar servisi + uçlar,
  `Auth/`) ✅ → activity-agent (30 gün kuralı) ✅ → lider (`/mcp`, araçlar, Program.cs — paylaşılan dosya) ✅ →
  web-agent (profilde anahtar yönetimi + formda 30 gün sınırı) ✅ → lider: Entegrasyonlar listesini günceller.
- Kabul senaryoları `docs/mcp-test-senaryolari.md` (MCP-xx), kalite kapısının listesine eklenir.
- Kabul: Claude Code'dan "Perşembe 19:00'a sinema ekle" → takvimde görünür; iptal edilen anahtarla 401.
- Uygulama notları (adım 3, 2026-11-09): SDK `ModelContextProtocol.AspNetCore` 2.3.1, durumsuz HTTP.
  Hafta/boş zaman sorguları `Application/Activities/AgendaService`'te (araç kodu ince adaptör).
  Boş zamanda tüm gün ve iptal edilen aktiviteler meşgul sayılmaz. Kurulum: `docs/mcp-kurulum.md`.
- Bilinen: araçların beklenen doğrulama hataları (McpException) SDK tarafından stack trace'li loglanır —
  gürültü, işlev sorunu değil; log seviyesi ileride ayarlanabilir.
