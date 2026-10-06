# 0019 - Application katmanında "şu anki kullanıcı" erişimi
**Durum:** Kabul edildi
**Tarih:** 2026-09-30

## Bağlam
ADR 0018 ile `UserId` artık istemciden alınmıyor, token'dan geliyor. Token'ı ASP.NET Core
çözer ve kimlik bilgisi `HttpContext.User` içinde durur. Oysa sahiplik kontrolleri
(`GetByIdForUserAsync(id, userId)`) ve rol kontrolleri Application katmanındadır.
Application ise ADR 0001 gereği HTTP'yi tanımaz. Bu durumda Application'ın `HttpContext`'e
bağımlı olmadan şu anki kullanıcıya nasıl erişeceğine karar vermemiz gerekti.

## Karar
- Application'da bir `ICurrentUser` interface'i tanımlanır. Bu interface `UserId` ve rol
  bilgisini (`IsAdmin`) sunar.
- Interface'in implementasyonu `HttpCurrentUser` adıyla **Api** katmanında yazılır. Sınıf
  `IHttpContextAccessor` üzerinden token claim'lerini okur ve DI kaydı Api'de yapılır.
- Servisler kullanıcı kimliğini metot parametresi olarak almaz, `ICurrentUser`'ı inject eder.
- `[Authorize]` controller'lara tek tek yazılmaz. `AddAuthorization` içinde
  `FallbackPolicy = RequireAuthenticatedUser()` tanımlanır, böylece her endpoint varsayılan
  olarak kilitli olur. Herkese açık endpoint'ler (register, login, e-posta doğrulama,
  refresh) açıkça `[AllowAnonymous]` ile işaretlenir.
- Kimlik bilgisi olmadan `ICurrentUser.UserId` okunursa `InvalidOperationException`
  fırlatılır. `ExceptionHandlingMiddleware` bu hatayı yakalar ve **500** döner (ADR 0013).

## Gerekçe
- **Implementasyonun Api'de olması:** `HttpContext` bir veri kaynağı değil, bir web
  detayıdır. Implementasyon Infrastructure'a konsaydı, Infrastructure'a
  `Microsoft.AspNetCore.App` referansı eklemek gerekirdi. Bu durumda veri katmanı web'e
  bağımlı hale gelirdi. Api zaten Application'ı referans ettiği için yeni bir bağımlılık
  gerekmiyor.
- **Parametre ile geçmek neden seçilmedi:** `GetMyActivities(userId)` yaklaşımı değerlendirildi.
  Bu yaklaşım açık ve test etmesi kolay. Ama iş kuralının doğruluğu controller'ın doğru
  id'yi geçmesine bağlı kalıyor, her metot imzası `userId` (ve sonra `isAdmin`) ile
  kirleniyor. `ICurrentUser` ile Application kimliği kendisi alır, controller araya giremez.
  Testte sahte bir `ICurrentUser` vermek yeterli.
- **FallbackPolicy:** Bu yaklaşımla `[AllowAnonymous]`'ı unutmanın bedeli "login çalışmıyor"
  olur. Bu hata hemen fark edilir. `[Authorize]`'ı unutmanın bedeli ise sessiz bir güvenlik
  açığıdır. Varsayılanı kapalı tutarak hatanın güvenli tarafta kalmasını sağlıyoruz.
- **Neden 401 değil de 500:** Fallback policy çalıştığı sürece kimliksiz istek kapıda 401
  alır ve Application'a hiç ulaşmaz. Application'a null kimlikle gelinmesi kullanıcının değil
  geliştiricinin hatasıdır (eksik ya da bozuk yapılandırma). 401 dönülseydi frontend
  kullanıcıyı login'e yönlendirir ve bug gizlenirdi. 500 ise hatayı error logu olarak görünür
  kılar. Kısaca: 401/403 kullanıcıya bir şey söyler, 500 bize bir şey söyler.

## Sonuçlar
- Application HTTP'den bağımsız kalır. Servis testleri `HttpContext` kurmadan, sahte bir
  `ICurrentUser` ile yazılır.
- Implementasyonlar iki katmana dağılır: altyapı implementasyonları Infrastructure'da,
  `HttpCurrentUser` Api'de. Bu bilinçli bir istisnadır.
- `ICurrentUser` HTTP isteği dışında (background job, hatırlatma gönderimi) anlamlı değildir.
  Bu tür akışlar kullanıcıyı `ICurrentUser`'dan değil, işlenen kaydın `UserId`'sinden alır.
- Her yeni herkese açık endpoint `[AllowAnonymous]` gerektirir.
- Kabul testleri: AUTH-48, AUTH-49 ([auth-test-senaryolari.md](../auth-test-senaryolari.md)).
