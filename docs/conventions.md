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

## Paylaşılan Dosyalar — SADECE TAKIM LİDERİ
Aşağıdaki dosyalara takım arkadaşları DOKUNAMAZ:
- Program.cs
- AppDbContext.cs
- DependencyInjection.cs (her katmandaki)
- *.csproj
- docker-compose.yml
Değişiklik gerekiyorsa takım liderine bildirilir (SendMessage, to: "main").

## Takım Lideri Adresi
Takım liderinin adresi "main"dir. Takım liderine bildirim, SendMessage ile
"main" adresine mesaj gönderilerek yapılır.
"team-lead", "lead", "takım lideri" gibi adlar ÇÖZÜLMEZ; mesaj ulaşmaz.

## Tasarım Kuralı
Basit başla. Spekülatif soyutlama yasak.
Rule of Three: aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarma.
