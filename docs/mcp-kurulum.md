# MCP Kurulumu: Claude'u AjandaAI'ye bağlamak

Kararlar: [ADR 0024](decisions/0024-mcp-sunucusu-ve-kisisel-erisim-anahtari.md). Senaryolar: [mcp-test-senaryolari.md](mcp-test-senaryolari.md).

## 1. Anahtar üret
Profil sayfası → "Claude için anahtar oluştur" → bir ad ver (örn. "Laptop - Claude Code").
Anahtar (`ajd_...`) **yalnızca bir kez** gösterilir; kopyala. Kaybedersen iptal edip yenisini üret.

> Profil ekranı hazır olana kadar: Swagger'da `POST /api/auth/tokens` (normal girişle "Authorize") aynı işi yapar.

## 2. Claude Code'a ekle
Backend çalışırken (`dotnet run --project backend/src/AjandaAI.Api --launch-profile http`):

```
claude mcp add --transport http --scope local ajandaai http://localhost:5250/mcp --header "Authorization: Bearer ajd_BURAYA_ANAHTAR"
```

- `--scope local`: ayar yalnızca senin makinende (`~/.claude.json`) durur. Anahtar repoya GİRMEZ —
  `--scope project` KULLANMA (`.mcp.json` commit'lenir, anahtar sızar).
- Kontrol: `claude mcp list` → `ajandaai ... √ Connected`.
- Dene: "Perşembe 19:00'a sinema ekle", "Bu hafta neler var?", "Cuma akşamı 2 saatlik boşluğum var mı?"

## 3. Claude Desktop (henüz denenmedi)
Şirket hesaplarında yönetici özel bağlantıları kapatmış olabilir. Açıksa yerel bir köprüyle (`mcp-remote`)
aynı adres ve başlık verilir; v2.3b'de doğrulanıp buraya yazılacak.

## Araçlar
| Araç | Ne yapar |
|---|---|
| `aktivite_ekle` | Tek aktivite ekler |
| `seri_ekle` | Tekrarlayan seri (en fazla 366 oluşum) |
| `haftayi_listele` | Bir haftanın aktiviteleri |
| `bos_zaman_bul` | Boş zaman aralıkları (en fazla 31 gün) |
| `kategorileri_listele` | Etkin kategoriler |

Sınırlar: anahtar başına dakikada 30 çağrı; başlangıç en fazla 30 gün geçmişte. Silme/güncelleme yok.

## Güvenlik
- Anahtar yalnızca `/mcp`'de geçer; normal API'ye giremez.
- Sızdığını düşünürsen profilden iptal et — anında geçersiz olur. Süresi 90 gün.
- Claude'un okuduğu aktiviteler sohbetin parçası olur (Anthropic modeline gider); şirket hesabıyla
  kullanıyorsan şirket hesabının sohbetlerinden geçer.

## v2.3b: hazır MCP'ler (ADR 0024 §3, §5)
Kod yazılmaz; her biri tek komutla Claude Code'a eklenir. Komutları **kendin** çalıştır (Claude kendi ayarını
değiştiremez). `--scope user`: tüm projelerde geçerli, ayar yalnızca senin makinende. Windows'ta `npx` tabanlı
sunucular `cmd /c` ile sarılır. Hepsi ücretsiz, anahtar istemez. Gereken: Node 22+ (makinede 24 var).

| Sunucu | Ne işe yarar | Yayıncı |
|---|---|---|
| `mongodb-mcp-server` | Log analizi (yalnızca okuma) | MongoDB (resmi) |
| `@modelcontextprotocol/server-memory` | Planlama tercihlerini hatırlar | MCP (resmi) |
| `open-meteo-mcp-server` | Hava durumu (Open-Meteo, anahtarsız) | Topluluk (cmer81, MIT) |
| `@cyanheads/openstreetmap-mcp-server` | Yer arama, adres ↔ konum (OpenStreetMap) | Topluluk (cyanheads, Apache-2.0) |

```
claude mcp add mongodb-loglar --scope user --env MDB_MCP_CONNECTION_STRING=mongodb://localhost:27018/ajandaai_logs --env MDB_MCP_READ_ONLY=true -- cmd /c npx -y mongodb-mcp-server@3.2.2

claude mcp add hafiza --scope user --env MEMORY_FILE_PATH=C:\Users\mehmet.kirimli\.ajandaai\hafiza.jsonl -- cmd /c npx -y @modelcontextprotocol/server-memory

claude mcp add hava --scope user -- cmd /c npx -y -p open-meteo-mcp-server open-meteo-mcp-server

claude mcp add harita --scope user -- cmd /c npx -y @cyanheads/openstreetmap-mcp-server@0.7.1
```
**Windows'ta zaman aşımı / `ENOENT package.json` hatası:** ilk açılışta npx indirmesi 30 saniyelik bağlanma süresini
aşarsa önbellekte yarım klasör kalır ve sonraki her deneme `ENOENT ... package.json` verir. Çözüm: yarım klasörü
sil (`%LOCALAPPDATA%\npm-cache\_npx\<kod>`), paketi kalıcı kur ve kaydı kurulu sürüme yönlendir. Harita için:
```
npm install -g @cyanheads/openstreetmap-mcp-server@0.7.1
claude mcp remove harita --scope user
claude mcp add harita --scope user -- cmd /c openstreetmap-mcp-server
```

Kontrol: `claude mcp list` → dördü de `Connected`. Önce `C:\Users\mehmet.kirimli\.ajandaai` klasörünü oluştur
(hafıza dosyası orada durur; npx önbelleğinde durursa güncellemede silinir).

**Bilinçli sınırlar**
- **Mongo:** veritabanında şifre yok; salt okunurluk MCP bayrağıyla (`MDB_MCP_READ_ONLY`) sağlanır. Canlıya
  çıkışta Mongo'ya kimlik doğrulama + salt okunur kullanıcı gelir. Loglarda maskeli kişisel veri var (ADR 0016).
- **Harita:** yer arama ve adres var, **yol süresi/rota YOK** (ücretsiz ve güvenilir rota sunucusu bulunamadı).
  OpenStreetMap kuralı: saniyede en fazla 1 istek, toplu sorgu yasak; normal sohbet kullanımında sorun değil.
- **Hava ve harita topluluk yapımı:** harita sürümü sabit (`@0.7.1`); hava sunucusunun yayınlanmış sürümü yok,
  her açılışta en yenisi gelir — şüphede kaldırılır (`claude mcp remove hava`).

**Deneme cümleleri**
- "Bugün kaç hata logu var, en sık hangisi?" · "Dün kaç başarısız giriş denemesi oldu?" · "En yavaş 5 istek hangisi?"
- "Cumartesi İstanbul'da hava nasıl? Güzelse sabah 10'a yürüyüş ekle."
- "Medicana Çamlıca'nın adresi ne?"
- "Sporu sabah yapmayı sevdiğimi hatırla." → sonra "Bu hafta spor için uygun saat bul."

**Log yapısı (Mongo `ajandaai_logs.logs`)**: `Timestamp`, `Level` (Information/Warning/Error/Debug),
`MessageTemplate`, `RenderedMessage`, `Properties` (`RequestPath`, `StatusCode`, `Elapsed`, `UserId`,
`ClientIp`, `RequestId`, `SourceContext`...). Her HTTP isteği bir Information kaydıdır; auth olayları
(kayıt, giriş, hatalı giriş, oturum yenileme) ve iş olayları ayrı mesajlarla yazılır.

## Geliştirme MCP'leri (2026-11-10)
Ürün değil, geliştirme yardımcısı: Claude Code'un bizim işimizi hızlandırması için. Hepsi ücretsiz, anahtarsız.

| Sunucu | Ne işe yarar | Yayıncı |
|---|---|---|
| `@playwright/mcp` | Tarayıcıyı açar, panelde tıklar, ekran görüntüsü alır (frontend kabulü) | Microsoft (resmi) |
| `@modelcontextprotocol/server-postgres` | Veritabanına salt okunur sorgu | MCP (referans; arşivlendi, çalışır) |
| `@upstash/context7-mcp` | Kütüphanelerin güncel belgeleri (Mantine, Google.GenAI, EF Core…) | Upstash |

```
claude mcp add playwright --scope user -- cmd /c npx -y @playwright/mcp@latest

claude mcp add postgres-okuma --scope user -- cmd /c npx -y @modelcontextprotocol/server-postgres postgresql://ajandaai_okuma:ajandaai_okuma@localhost:5434/ajandaai

claude mcp add context7 --scope user -- cmd /c npx -y @upstash/context7-mcp
```

**Bilinçli sınırlar**
- **Postgres:** uygulamanın kullanıcısı (`ajandaai`) VERİLMEZ. Ayrı `ajandaai_okuma` rolü
  (`docker/postgres-init/02-okuma-kullanicisi.sql`): yalnızca SELECT; token tabloları ve `users.password_hash`
  okunamaz. Yeni tablolar otomatik okunabilir olur (default privileges). Şifre yerel geliştirme içindir.
- **Playwright:** ilk açılışta tarayıcı indirir (birkaç yüz MB); `ENOENT` çıkarsa yukarıdaki npx önbellek çözümü.
- **Context7:** anahtarsız kullanımda istek sınırı var; normal kullanımda yeter.
