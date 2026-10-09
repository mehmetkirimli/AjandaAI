# 0018 - Authentication ve yetkilendirme (v2)
**Durum:** Kabul edildi (uygulandı: 2026-10-09, P1–P8)
**Tarih:** 2026-09-28

## Bağlam
v1'de authentication yok: `POST /api/users` herkese açık ve şifre almıyor; `ActivityCreateDto`
içinde `UserId` istemciden geliyor, yani istemci "ben 5 numaralı kullanıcıyım" diyebiliyor.
docs/database.md ve ADR 0005, v2 için tek kuralı önceden koymuştu: kullanıcıya ait kayıtları
dönen HER sorguda UserId kontrolü, yoksa IDOR açığı.

Auth, v2 listesindeki (docs/domain.md) davet sistemi, MCP entegrasyonu, analiz ajanı ve audit
log için önkoşuldur; v2'nin ilk adımıdır.

İstemciler: önce tarayıcıda çalışan bir web uygulaması (Angular/React, henüz seçilmedi), sonra
mobil uygulama, ayrıca makine istemcileri (MCP, analiz ajanı). Hepsi aynı sunucuda yayınlanacak.
Swagger bir istemci değil, geliştirme aracıdır.

## Karar

### 1. Mekanizma: JWT
Kimlik doğrulama JWT (Bearer) ile yapılır. Web, mobil ve makine istemcileri aynı mekanizmayı
kullanır. Cookie tabanlı session kullanılmaz.

### 2. Token modeli
| | |
|---|---|
| Access token | JWT, 15 dakika, stateless. Claim'ler: kullanıcı Id, rol |
| Refresh token | Kriptografik rastgele değer (64 byte), 30 gün |
| Saklama | Refresh token Postgres'te SHA-256 hash'i ile saklanır, düz metin saklanmaz |
| Taşıma | Tüm istemcilerde request body (cookie yok) |
| Rotation | Her yenilemede yeni refresh token üretilir, eskisinin `RevokedAt` alanı doldurulur (silinmez; bool yerine tarih, ne zaman revoke edildiği de bilinsin) |
| Hırsızlık tespiti | Revoke edilmiş bir refresh token tekrar kullanılırsa kullanıcının TÜM refresh token'ları revoke edilir |
| Logout | Refresh token revoke edilir; access token en fazla 15 dakika daha geçerli kalır |
| Rol okuma | Login ve refresh, rolü token'dan değil DB'den okur |

### 3. Sahiplik kontrolü sorgu içindedir
- Kullanıcıya ait kayıtlar sahiplik koşuluyla sorgulanır: `GetByIdForUserAsync(id, userId)` →
  `WHERE id = @id AND user_id = @me` (katılımcılar gelince: `OR katılımcı`).
- Başkasının kaydı sorgudan hiç dönmez; serviste unutulabilecek ayrı bir `if` yoktur.
- Kullanıcı endpoint'lerinde `isAdmin` istisnası YOKTUR (bkz. Karar 6).
- `UserId` artık istemciden alınmaz; token'dan gelir. DTO'lardaki `UserId` alanları kaldırılır.
- Başkasının kaydına erişim denemesi dışarıya **404** döner, gövdesi gerçek 404 ile byte byte
  aynıdır. Frontend de iki durumu ayırt edemez ve tek bir "bulunamadı" ekranı gösterir.
- İçeride: sorgu boş dönerse, yalnızca bu hata yolunda `ExistsAsync(id)` çalışır; kayıt varsa
  "yetkisiz erişim denemesi" Warning olarak loglanır (`{UserId} {ActivityId}`, ADR 0016).

### 4. Mevcut veri
Mevcut kullanıcılar test verisidir ve silinir. `PasswordHash` NOT NULL olarak eklenir.

### 5. Roller
- `User.Role` enum'u: `User`, `Admin`. Veritabanında string (ADR 0002).
- Register rol kabul etmez; her yeni kullanıcı `User` olur. Body'de `role` gönderilse de yok sayılır.
- İlk admin DB üzerinden elle atanır. Public bir "admin ol" akışı hiçbir zaman olmayacak.
- Admin paneli ileride kullanıcı oluştururken rol atayabilir.

### 6. Admin yetkileri
Admin işlemleri ayrı controller'lardan geçer: `/api/admin/...`. Kullanıcı endpoint'leri katı kalır.

| İşlem | v2 ilk sürüm |
|---|---|
| Kategori ekleme, güncelleme, pasife alma, okuma | ✅ |
| Kullanıcı listeleme, ekleme, güncelleme, pasife alma | ✅ |
| Aktivite ve hatırlatma **üstverisini** okuma | ✅ (Id, UserId, Status, Start, CreatedAt, RemindAt, SentAt...) |
| Aktivite ve hatırlatma **içeriğini** okuma (Title, Description, Note) | ❌ Admin DTO'larında bu alanlar YOK |
| Aktiviteyi pasife alma (moderasyon) | ✅ |
| Kullanıcı adına düzenleme (destek talebi) | ❌ Audit log geldikten sonra, gerçek bir talep olursa |

### 7. Rol değişikliği
- Terfi: kullanıcı tekrar login olduğunda veya token yenilendiğinde etkili olur.
- Rol düşürme: kullanıcının tüm refresh token'ları revoke edilir; en geç 15 dakika içinde düşer.
  Bu 15 dakikalık pencere kabul edilmiştir.

### 8. Kayıt ve e-posta doğrulaması
- Doğrulama e-posta ile yapılır (SMS değil).
- `User.EmailConfirmedAt` (`DateTimeOffset?`): null = doğrulanmadı. `IsActive`'ten AYRI bir alandır.
- Doğrulanmamış kullanıcı login olamaz.
- Doğrulama linkindeki token: rastgele, tek kullanımlık, 12 saat geçerli, DB'de SHA-256 hash'i
  ile saklanır.

| Kayıt denemesinde e-posta durumu | Sonuç |
|---|---|
| Hiç yok | Yeni kayıt, doğrulama maili gönderilir |
| Var, doğrulanmamış | Eski kayıt HARD delete edilir, yeni kayıt yerine geçer, eski link geçersiz olur |
| Var, doğrulanmış (aktif veya pasif) | Kayıt oluşmaz, gelen kutusuna "zaten hesabınız var" maili gider |

- Doğrulanmamış kayıtların periyodik temizliği v2 ilk sürümde YOK; bildirim altyapısıyla
  (arka plan servisi) birlikte gelir.
- Gönderim soyutlanır: `IEmailSender` (Application).
  - `LogEmailSender`: linki log'a yazar. YALNIZCA Development'ta kayıtlıdır.
  - `SmtpEmailSender`: v2 bildirim adımında gelir.

### 9. Hesap sızdırmama (enumeration) ve timing
- Register her durumda aynı yanıtı döner: `202 "Doğrulama e-postası gönderildi."`. Farkı
  sadece gelen kutusunun sahibi görür.
- Login her hata durumunda aynı mesajı döner: "E-posta veya şifre hatalı." ("Şifremi unuttum"
  geldiğinde de aynı ilke uygulanır.)
- Yanıt süreleri eşitlenir:
  - Login'de kullanıcı yoksa sabit bir dummy hash'e karşı doğrulama yine çalışır.
  - Register'da kayıtlı e-posta yolunda da gelen şifre hash'lenir, sonuç atılır.
  - Mail gönderimi yanıtı beklemeden arka planda yapılır.
- Rate limiting: register ve login'e IP bazlı sınır; ASP.NET Core'un yerleşik (bellek içi)
  rate limiter'ı kullanılır.

### 10. Şifre kuralları ve hash
| Kural | Değer |
|---|---|
| Minimum uzunluk | 10 karakter |
| Maksimum uzunluk | 128 karakter |
| Karmaşıklık kuralı (büyük harf, özel karakter) | YOK |
| Sık kullanılan şifreler | Reddedilir (yerel 10.000'lik liste) |
| Kişisel bilgi | Şifre e-postanın ilk kısmını veya görünen adı içeremez |
| Boşluk, Türkçe karakter | Serbest; şifre `Trim()` edilmez |
| Periyodik değiştirme zorunluluğu | YOK |

- Kullanıcı mesajı: "Şifreniz en az 10 karakter olmalı. Uzun bir cümle kullanabilirsiniz.
  Çok yaygın şifreler kabul edilmez."
- Hash: `PasswordHasher<T>` (`Microsoft.Extensions.Identity.Core`, PBKDF2 + HMAC-SHA512).
  ASP.NET Identity'nin tamamı kurulmaz. `SuccessRehashNeeded` sonucunda şifre yeniden hash'lenir.

## Gerekçe
- **JWT:** İstemcilerin çoğu (mobil, MCP, analiz ajanı) tarayıcı değil; cookie jar'ları yok.
  Bugün cookie seçip mobil gelince JWT'ye geçmek iki auth mekanizmasının bakımı demekti.
- **İki token:** Kısa ömürlü tek token kullanıcıyı sürekli şifre girmeye zorlar; uzun ömürlü tek
  token çalındığında saldırgana uzun süre erişim verir. Kısa access token + iptal edilebilir uzun
  refresh token ikisini birlikte çözer.
- **SHA-256 (refresh token) ile PBKDF2 (şifre) farkı:** İnsanların seçtiği şifreler tahmin
  edilebilir, sızıntı listeleriyle denenir; yavaş hash her denemeyi pahalılaştırır. 64 byte
  rastgele değer ise tahmin edilemez, hızlı hash saldırgana avantaj sağlamaz. Ayrıca SHA-256
  deterministiktir: `WHERE token_hash = @hash` ile index'li arama yapılabilir; tuzlu bir hash'le
  bu imkânsızdır.
- **Revoke (silmek yerine):** Silinen token'ın bir zamanlar var olduğu bilinemez; rotation'daki
  hırsızlık tespiti bu kayda dayanır.
- **Body ile taşıma:** Tek sözleşme, tek test seti. Bedeli aşağıda (Sonuçlar).
- **Sorgu içinde sahiplik:** İki seçenek değerlendirildi: (A) kaydı getirip C#'ta kontrol etmek,
  (B) koşulu sorguya gömmek. A kontrolün her serviste çağrılmasını hatırlamaya dayanır ve
  katılımcılar gelince ek sorgu gerektirir. B tek sorguda çözer ve unutulamaz. B seçildi.
- **403 yerine 404:** 403 dönmek "bu Id'de bir kayıt var" bilgisini sızdırır; saldırgan Id'leri
  deneyerek hangi kayıtların var olduğunu çıkarır. Frontend'in bildiği her şeyi kullanıcı da bilir
  (tarayıcı geliştirici araçları); ayrım yalnızca sunucu loglarında yaşayabilir.
- **Ayrı admin controller'ları:** Admin'i kullanıcı sorgularına `OR @isAdmin` ile eklemek güvenlik
  filtresinin içine bir bypass açar ve her sorguyu etkiler.
- **İçerik değil üstveri:** Admin'in ihtiyacı sistemin sağlıklı çalıştığını görmektir (hatırlatma
  kuruldu mu, gönderildi mi), kullanıcının hayatını okumak değil. KVKK ölçülülük ilkesi. Maskelemek
  yerine alanları hiç seçmemek tercih edildi: seçilmeyen veri loga veya bir bug yüzünden dışarı
  sızamaz (veri minimizasyonu). Operasyonel sorulara Mongo logları da cevap verir (ADR 0016).
- **15 dakikalık rol penceresi:** Admin sayısı çok az (pratikte tek); her admin isteğinde rolü
  DB'den okumak bu risk için gereksiz bir maliyet.
- **E-posta (SMS değil):** Mesaj başına maliyet yok, `Email` alanı zaten var.
- **`EmailConfirmedAt`, `IsActive`'ten ayrı:** `IsActive` soft delete anlamı taşır (ADR 0006). İkisi
  aynı alanı paylaşırsa pasife alınmış bir kullanıcı doğrulama linkiyle kendini geri açabilirdi.
  Tarih tipi, bool'un bilgisine ek olarak doğrulama zamanını da verir.
- **Doğrulanmamış kaydın hard delete edilmesi:** Doğrulanmamış kullanıcı login olamadığı için ona
  FK veren kayıt oluşamaz; ADR 0006'nın "FK verilmeyen entity hard delete edilir" kuralıyla tutarlıdır.
  Üzerine yazma, adres rehin alma sorununu çözer: başkasının adresiyle kaydolup doğrulamayan biri,
  gerçek sahibinin kaydını engelleyemez.
- **Aynı yanıt ve eşit süre:** Kayıtlı e-posta listesi credential stuffing (başka sitelerden sızan
  e-posta ve şifre çiftlerinin denenmesi) ve hedefli oltalama için değerlidir. Yanıt metni aynı olsa
  bile süre farkı (hash çalışıyor ya da çalışmıyor) aynı bilgiyi sızdırır (timing attack).
- **Karmaşıklık kuralı yok:** Karmaşıklık kuralı güçlü şifre değil, `Password1!` gibi kalıplar
  ürettirir; saldırgan listeleri bu kalıba göre hazırlanır. NIST SP 800-63B uzunluğu ve sızmış
  şifre kontrolünü önerir, karmaşıklık kuralını ve periyodik değiştirmeyi önermez.
- **BCrypt yerine PBKDF2:** BCrypt şifrenin yalnızca ilk 72 byte'ını kullanır, gerisini sessizce
  yok sayar. Türkçe karakterler UTF-8'de 2 byte olduğundan sınır pratikte daha da düşer. PBKDF2'de
  bu sınır yoktur; `PasswordHasher<T>` Microsoft tarafından bakılır ve rehash yolu sunar.

## Sonuçlar
- Mevcut endpoint'lerin çoğu değişir: `UserId` DTO'lardan çıkar, sorgular sahiplik koşulu alır,
  `POST /api/users` yerini `/auth/register`'a bırakır.
- Application'ın "şu anki kullanıcı kim" bilgisine ihtiyacı var ama `HttpContext`'i tanıyamaz
  (ADR 0001). Çözümü: [ADR 0019](0019-current-user-erisimi.md).
- **Bilinen risk:** Web'de refresh token'ın JavaScript'in erişebildiği bir yerde saklanması
  gerekecek; bir XSS açığı token'ı sızdırabilir. Azaltan önlemler: rotation ve yeniden kullanım
  tespiti, frontend'de CSP. Geri dönüş yolu: web için HttpOnly cookie eklemek mevcut sözleşmeyi
  kırmaz, yeni bir seçenek ekler.
- **Bilinen risk:** Rol düşürmede 15 dakikalık pencere.
- **Bilinçli tercih:** Logout ile revoke edilmiş bir refresh token tekrar kullanılırsa bu da
  yeniden kullanım sayılır ve kullanıcının tüm oturumları kapanır. Logout'u ayrı işaretlemek
  (`RevokedReason`) değerlendirildi, reddedildi: ara sıra gereksiz çıkışın bedeli, çalınmış bir
  token'ın sessizce reddedilip fark edilmemesinden düşüktür. (2026-10-09)
- **Bilinen kısıt:** İlk admin elle SQL ile atanır; her yeni ortamda (yeni geliştirme ortamı,
  canlıya ilk çıkış) tekrarlanmalıdır. Son admin kendini User'a düşürürse düzeltme de DB'den yapılır.
- **Bilinen kısıt:** Rate limiter bellek içidir; birden fazla sunucuya geçilirse ortak bir sayaç
  (ör. Redis) gerekir.
- **Bilinen kısıt:** `LogEmailSender` log'a gizli bir link yazar; Production'da kayıtlı olmaması
  zorunludur.
- **Bilinen kısıtlar (final review, 2026-10-09).** Dağıtım şekli netleşince ele alınacaklar:
  - Rate limiter `RemoteIpAddress` ile bölümlenir. Reverse proxy arkasında tüm istemciler tek IP
    görünür ve 10 istek herkesin login'ini kilitler. Proxy'ye geçerken `UseForwardedHeaders`,
    `KnownProxies`/`KnownNetworks` ile birlikte açılmalıdır; aksi halde `X-Forwarded-For` taklidiyle
    limit aşılır.
  - Login'de e-posta/şifre uzunluk sınırı yok (register'da 256/128 var). Pratik etkisi küçük.
  - Register'da yeni kayıt yolunda ek bir INSERT var, kayıtlı yolda yok. Hash baskın olduğu için
    süre farkı küçük; istatistiksel ölçüm teorik olarak mümkün.
  - Kabul edilenler: admin PUT ile e-posta doğrulaması aynı anda olursa `Update(entity)` doğrulamayı
    ezebilir (dar pencere); pasife alınmış ama doğrulanmamış bir kayıt aynı e-postayla yeniden
    register edilince yeni ve aktif bir kayıt oluşur; Swagger'da Bearer tanımı yok; zaman kaynağı
    servisler arasında tutarsız (`UtcNow` / `TimeProvider`); refresh token'lar birikir, temizlik
    bildirim altyapısıyla gelir.
- JWT imzalama anahtarı git'e girmez; Production'da environment variable olarak verilir (ADR 0014).
- Kabul testleri: [docs/auth-test-senaryolari.md](../auth-test-senaryolari.md).
