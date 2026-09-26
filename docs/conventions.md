# Conventions

## Dosya Başı Comment
Her .cs dosyasının en üstünde, o dosyanın ne işe yaradığını anlatan
maksimum 4 satırlık bir comment bloğu olur.

## İsimlendirme
- Interface: I ile başlar (IActivityRepository)
- Async metodlar: Async ile biter (GetByIdAsync)
- DTO: {Entity}{Amaç}Dto (ActivityCreateDto, ActivityListDto)
- Entity'ler tekil (Activity), DbSet'ler çoğul (Activities)

## Ortak Response Yapısı
Tüm controller'lar ApiResponse<T> döner:
  Success (bool), Data (T), Message (string), Errors (List<string>)
Ham entity dönmek YASAK. Her zaman DTO döner.

### ApiResponse<T> + ResultType
ApiResponse<T> ayrıca bir ResultType taşır (Application/Common/ResultType.cs).
ResultType [JsonIgnore]'dur, JSON'a yazılmaz; yalnızca iç kullanım içindir.
Servisler sonucu yardımcılarla üretir:

| Yardımcı                  | ResultType      | HTTP |
|---------------------------|-----------------|------|
| Ok(data, message)         | Success         | 200  |
| Created(data, message)    | Created         | 201  |
| NoContent(message)        | NoContent       | 204  |
| Fail(message, errors)     | ValidationError | 400  |
| NotFound(message)         | NotFound        | 404  |
| Conflict(message)         | Conflict        | 409  |
| Error(message, errors)    | Error           | 500  |

Servis kuralları:
- Kayıt bulunamadı → NotFound (null DÖNÜLMEZ)
- Validator hatası → Fail
- POST başarılı → Created
- DELETE başarılı (soft/hard) → Ok

## Controller Kuralları
Controller metodları tek satırdır: servisi çağırır, sonucu döner.
ActionResult KULLANILMAZ, dönüş tipi Task<ApiResponse<T>>'dir.
[ProducesResponseType] attribute'u KULLANILMAZ.
HTTP status kodu ApiResponseFilter tarafından belirlenir.

İleride API dışarıya yayınlanır ve Swagger dokümantasyonu gerekirse,
status kodları tek tek attribute ile değil, bir IOperationFilter ile
merkezi olarak eklenir.

### Status kodu tek kaynaktan belirlenir
HTTP status kodunu Api/Filters/ApiResponseFilter.cs (IAsyncResultFilter),
ResultType'a bakarak belirler. Status kodu response body'ye YAZILMAZ
(tek kaynak ilkesi: status yalnızca HTTP katmanında yaşar).
NoContent'te 204 gövde taşıyamadığı için filter gövdesiz sonuç döner.

### Exception'lar
Servisler exception FIRLATMAZ; beklenen hatalar ResultType ile döner.
Beklenmeyen exception'ları Api/Middleware/ExceptionHandlingMiddleware.cs
yakalar ve ApiResponse.Error("Beklenmeyen bir hata oluştu.") ile 500 döner.
Exception mesajı ve stack trace YALNIZCA Development ortamında errors'a
eklenir; Production'da ham hata sızdırılmaz.

## Hata Mesajları
Hata mesajları iç tip adı, namespace, stack trace veya dosya yolu
İÇERMEZ. Bu detaylar sadece Development ortamında gösterilir.
Kullanıcıya dönen mesajlar Türkçe ve anlaşılır olmalıdır.

Uygulama: ExceptionHandlingMiddleware (exception detayı) ve Program.cs'teki
InvalidModelStateResponseFactory (model-binding hataları: "{alan} alanı geçersiz."
veya "Geçersiz istek gövdesi.") bu kurala göre ortam kontrolü yapar.

## Modül Deseni — ÇAKIŞMA ÖNLEME
Servis kayıtları Program.cs'e veya ortak DependencyInjection.cs'e YAZILMAZ.

Her kaynak kendi modül dosyasını alır:
  Application/{Kaynak}/{Kaynak}Module.cs  → IModule implement eder
  (IModule: Application/Common/IModule.cs)

Kayıtların yeri:
- Servis kayıtları Application/{Kaynak}/{Kaynak}Module.cs'te yapılır.
- Repository kayıtları Infrastructure/DependencyInjection.cs'te yapılır
  (Application, Infrastructure'daki repository sınıflarını göremez).
- Tek implementasyonu olan servisler için interface yazılmaz; somut sınıf
  doğrudan kaydedilir: services.AddScoped<CategoryService>()

Infrastructure/DependencyInjection.cs, Application assembly'deki tüm IModule'leri
otomatik toplar (modüllerin parametresiz constructor'ı olmalı).
Bu dosyaya sadece takım lideri dokunur; yeni repository kaydı gerekiyorsa
takım liderine bildirilir (SendMessage, to: "main" — bkz. Takım Lideri Adresi).

Aynı kural EF configuration için de geçerlidir:
  Infrastructure/Persistence/Configurations/{Entity}Configuration.cs

## Doğrulama (Validation)
Girdi doğrulaması FluentValidation ile yapılır (paket: AjandaAI.Application).
Validator dosyaları: Application/{Kaynak}/Validators/{Dto}Validator.cs
  Örnek: Application/Categories/Validators/CategoryCreateDtoValidator.cs
  (sınıf adı: CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>)

Otomatik pipeline KULLANILMAZ (AddFluentValidationAutoValidation yok).
Servis validator'ı constructor'dan IValidator<XCreateDto> olarak alır ve
ValidateAsync ile kendisi çağırır.

Hata durumunda ApiResponse<T>.Fail(message, errors) döner, exception FIRLATILMAZ.
errors, ValidationResult.Errors içindeki ErrorMessage değerlerinin listesidir:
  var result = await _validator.ValidateAsync(dto, cancellationToken);
  if (!result.IsValid)
      return ApiResponse<T>.Fail("Doğrulama hatası.",
          result.Errors.Select(e => e.ErrorMessage).ToList());

Validator kaydı kaynağın kendi Module.cs dosyasında yapılır:
  services.AddScoped<IValidator<XCreateDto>, XCreateDtoValidator>();

Gerekçe: otomatik pipeline Program.cs'e kayıt gerektirir (paylaşılan
dosya, çakışma riski). Elle çağırma her kaynağın kendi modülünde kalır.

## Silme Politikası
Başka bir kaydın FK verdiği entity'ler SOFT delete edilir (IsActive = false).
Hiçbir kaydın FK vermediği entity'ler HARD delete edilir.

Mevcut durum:
- Category: soft (Activity FK veriyor)
- User: soft (Activity FK veriyor)
- Activity: soft (Reminder FK veriyor)
- Reminder: hard (kimse FK vermiyor)

Soft delete edilen bir entity'ye FK veren tüm validator'lar,
ExistsAsync değil IsActiveAsync kullanır. Pasif kayda yeni
bağlantı kurulamaz.

## Paylaşılan Dosyalar
Aşağıdaki dosyalara takım arkadaşları VARSAYILAN OLARAK dokunamaz:
- Program.cs
- AppDbContext.cs
- DependencyInjection.cs (her katmandaki)
- *.csproj
- docker-compose.yml

İSTİSNA: Altyapı görevlerinde (paket ekleme, servis kaydı, container tanımı)
bu dosyalara dokunmak işin doğasıdır. Bu durumda:
- Görev tanımında hangi dosyalara dokunulacağı AÇIKÇA yazılır
- İzin tek seferlik ve o göreve özeldir
- Teammate izin yoksa DURUR ve takım liderine ("main") bildirir
  (SendMessage, to: "main")

## Log Kuralları
Structured logging kullanılır, string interpolation KULLANILMAZ.
  DOĞRU:  _logger.LogInformation("Aktivite oluşturuldu {ActivityId} {UserId}", id, userId);
  YANLIŞ: _logger.LogInformation($"Aktivite oluşturuldu {id}");
Gerekçe: structured logging Mongo'da sorgulanabilir alanlar üretir.

Kişisel veri MASKELENEREK loglanır, asla açık yazılmaz:
email, telefon, adres, ad-soyad.
MaskingHelper kullanılır (Application/Common/Logging/MaskingHelper.cs):
  _logger.LogInformation("Kullanıcı oluşturuldu {UserId} {Email}",
      user.Id, MaskingHelper.MaskEmail(user.Email));
Serilog destructuring policy (Api/Logging/SensitiveDataDestructuringPolicy.cs)
güvenlik ağıdır, ona güvenilmez; elle maskeleme esastır.

Seviyeler:
- Information: başarılı iş olayları
- Warning: doğrulama hataları, beklenen başarısızlıklar (detayıyla)
    _logger.LogWarning("Kategori oluşturma doğrulama hatası {@Errors}", errors);
- Error: beklenmeyen hatalar

## Takım Lideri Adresi
Takım liderinin adresi "main"dir. Takım liderine bildirim, SendMessage ile
"main" adresine mesaj gönderilerek yapılır.
"team-lead", "lead", "takım lideri" gibi adlar ÇÖZÜLMEZ; mesaj ulaşmaz.

## Tasarım Kuralı
Basit başla. Spekülatif soyutlama yasak.
Rule of Three: aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarma.

## Sayfalama
Tüm liste endpoint'leri PagedResult<T> döner.
Varsayılan sayfa boyutu 20, maksimum 100'dür.
Geçersiz sayfa parametreleri sessizce sınıra çekilir, hata dönülmez.
Filtre parametreleri [FromQuery] ile alınır ve opsiyoneldir.
