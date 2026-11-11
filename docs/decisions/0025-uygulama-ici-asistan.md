# 0025 - Uygulama içi asistan (chatbot)
**Durum:** Kabul edildi (2026-11-09; test senaryoları yazılacak)
**Tarih:** 2026-11-09

## Bağlam
v2.3'te Claude Code, MCP üzerinden ajandayı yönetebildi (ADR 0024). Ürün sahibi aynı deneyimi uygulamanın
içinde, panelden bir sohbet kutusuyla istiyor. Bunun için backend'in bir dil modelini kendisinin çağırması gerekir.
Kısıtlar:
- Ürün sahibinin Anthropic API anahtarı yok (Claude erişimi şirket Team aboneliğiyle; API ayrı ve ücretli).
- Maliyet hassasiyeti yüksek.
- Ürün sahibinin Google AI Pro aboneliği var; bu bir tüketici aboneliğidir, Gemini API'den ayrıdır (doğrulanmadı:
  AI Studio projede "Free"/"Tier 1" katmanını gösterir).

## Karar

### 1. Sağlayıcı: Gemini API, ücretsiz katman (ürün sahibi kararı)
- Paket `Google.GenAI` 2.0.0 (googleapis, net8.0). Anahtar AI Studio'dan, ürün sahibinin Google hesabıyla.
- Model ayardan okunur (`Assistant:Model`; 2026-11-10 denemesi: `gemini-3.5-flash` ücretsiz katmanda çalışıyor, `3.8-flash` yoğunlukta 503,
  `2.5-flash` yeni kullanıcıya kapalı).
- **Kota gerçeği (2026-11-11 kabul denemesi):** `gemini-3.5-flash` ücretsiz katmanda proje başına **günde 20 istek**;
  bir sohbet mesajı araç turlarıyla 2–4 istek harcar (≈ günde 5–10 mesaj, tüm kullanıcılar için ortak). Bu yüzden
  ana model `gemini-3.5-flash-lite` (daha hızlı, kotası daha geniş), yedek `gemini-3.5-flash` (`Assistant:FallbackModel`).
  Yoğunluk/kota (429/5xx): 1,5 sn sonra bir kez daha, sonra yedek model, sonra 503. SDK'nın kendi yeniden denemesi
  kapalı (varsayılanı 5 deneme × 60 sn'ye kadar beklemeydi → cevap ~2 dk sürüyordu), istek zaman aşımı 30 sn.
  Sonuç: kullanıcı başına "günde 200" sınırı ücretsiz katmanda pratikte ulaşılamaz; gerçek tavan Google kotasıdır.
- Anahtar repoya GİRMEZ: Development'ta `dotnet user-secrets` (`Assistant:ApiKey`), Production'da ortam değişkeni.
  Anahtar yoksa asistan uçları "asistan yapılandırılmadı" der; uygulamanın geri kalanı etkilenmez.
- **Veri koşulu (bilinçli kabul):** ücretsiz katmanda içerik Google ürünlerini geliştirmek için kullanılabilir ve insanlar
  okuyabilir; Google "kişisel/hassas bilgi göndermeyin" der. Bu aşama demo verisiyle kullanılır. Gerçek kişisel planlar
  ve uygulamanın başkalarına açılması ücretli katman (veya başka sağlayıcı) gerektirir — ayrıca şartlar ücretsiz
  katmanı tüketici uygulaması için değil geliştirme için tanımlar.

### 2. Sağlayıcıdan bağımsız mimari
- Sohbet `Microsoft.Extensions.AI` `IChatClient` soyutlaması üzerinden; Gemini bir uygulamasıdır
  (`AsIChatClient(...)`, `UseFunctionInvocation()`). Claude veya başka bir modele geçiş yalnızca Infrastructure'daki
  kayıt + ayar değişikliğidir; araçlar, uçlar, arayüz değişmez.
- Katmanlar (ADR 0001/0012): araç tanımları ve asistan servisi Application'da (sağlayıcı tipi görmez);
  `IChatClient` kaydı Infrastructure'da; uç Api'de.

### 3. Araçlar: ilk sürümde hepsi açık, profile göre aç/kapa altyapısı şimdiden
- Ajanda: haftayı listele, boş zaman bul, kategorileri listele, tatilleri listele (okuma) + **aktivite öner, seri öner**.
- **Her ekleme onaya sunulur (ürün sahibi kararı, 2026-11-10):** asistan veritabanına YAZMAZ. Öneri araçları girdiyi
  web ile aynı doğrulamadan geçirir (30 gün kuralı dahil) ve yapılandırılmış bir öneri döner; panel bunu
  [Deftere yaz] [Değiştir] [Vazgeç] kartı olarak gösterir. "Deftere yaz" mevcut `POST /api/activities` /
  `/series` uçlarını çağırır. Böylece modelin elinde hiç yazma yetkisi olmaz ve kayıt yolu tektir.
- Hava durumu: Open-Meteo (anahtarsız) backend'den.
- Resmî tatiller: Nager.Date (`date.nager.at`, anahtarsız) backend'den, ülke `TR`, yıl bazında önbellek
  (ürün sahibi kararı, 2026-11-10). "Bayramda boşum", "tatile toplantı koyma" için. Dinî bayramların listede
  olduğu gerçek çağrıyla doğrulanır; yoksa araç açıklamasında belirtilir.
- Yer arama: OpenStreetMap Nominatim backend'den — saniyede en fazla 1 istek, uygulamayı tanıtan User-Agent,
  sonuç önbelleği (Nominatim kullanım politikası). Rota/süre YOK.
- Her aracın sabit bir anahtar adı vardır; etkin araç listesi tek bir yerden belirlenir. İlk sürümde hepsi açık;
  rol/profil bazında aç/kapa sonraki turdur (admin ekranı).
- **Admin'e özel araçlar (ürün sahibi kararı, 2026-11-10):** log sorgulama (Mongo, salt okunur) ve veritabanı
  sorgulama (`ajandaai_okuma` rolü: yalnızca SELECT, token tabloları ve şifre özeti kapalı). İkisi de tüm
  kullanıcıların verisini gösterir; bu yüzden rolü `User` olan kullanıcıya hiç sunulmaz, çağrılırsa reddedilir.
  Sunucu tarafı kontrol esastır (modelin talimatına güvenilmez).
- **Hafıza kişi bazlı:** her kullanıcının kendi "asistan notları" vardır (örn. "sporu sabah severim"); başka kullanıcı
  göremez. Kullanıcı notlarını profilinden görür, ekler, değiştirir, siler. Asistan notları okur; yeni not önerirse
  bu da onaya sunulur ("Hatırlayayım mı?"). Claude Code'daki `server-memory` (tek dosya, tek kişi) bununla
  ilgisizdir; o yalnızca geliştirme makinesinde durur.
- Kapsam dışı: aktivite silme/güncelleme.

### 4. Sohbet
- `POST /api/assistant/chat` (JWT): kullanıcı mesajı + istemcinin tuttuğu önceki mesajlar. İlk sürümde konuşma
  sunucuda saklanmaz (sayfa yenilenince sıfırlanır).
- Sistem talimatı: Türkçe, kullanıcının saat dilimi ve "bugün", yalnızca verilen araçlar, emin değilse sor,
  kayıt eklemeden önce özetle.
- Sınırlar (ürün sahibi onayı): kullanıcı başına dakikada 10, günde 200 mesaj (ücretsiz katman kotasını korur);
  tek istekte en fazla 8 araç çağrısı; mesaj ≤ 2.000 karakter, geçmiş ≤ 20 mesaj. İleride: yüksek kota "premium"
  üyelikle ücretlendirilebilir (fikir, kapsam dışı).
- Loglama: kullanıcı mesajı içeriği LOGLANMAZ (ADR 0016); araç adı, süre, token sayısı loglanır.

### 5. Arayüz
- Eskiz defter diliyle (ADR 0021 Karar 5f) sağdan açılan sohbet paneli; taslak onaydan sonra web-agent'a.

## Gerekçe
- Anahtarsız ve ücretsiz başlamanın tek gerçekçi yolu Gemini ücretsiz katmanı.
- `IChatClient` soyutlaması sağlayıcı kararını geri alınabilir kılar; veri koşulu veya kota sorun olursa kod yeniden
  yazılmaz.

## Sonuçlar
- İş sırası (ADR 0020): lider (paket + IChatClient kaydı + Program.cs) → activity/catalog alanında araçlar ve asistan
  servisi → web-agent (sohbet paneli). Kabul: panelden "Cumartesi hava güzelse 10'a yürüyüş ekle" → hava aracı +
  boş zaman + ekleme, takvimde görünür.
