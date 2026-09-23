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
takım liderine bildirilir.

Aynı kural EF configuration için de geçerlidir:
  Infrastructure/Persistence/Configurations/{Entity}Configuration.cs

## Paylaşılan Dosyalar — SADECE TAKIM LİDERİ
Aşağıdaki dosyalara takım arkadaşları DOKUNAMAZ:
- Program.cs
- AppDbContext.cs
- DependencyInjection.cs (her katmandaki)
- *.csproj
- docker-compose.yml
Değişiklik gerekiyorsa takım liderine bildirilir.

## Tasarım Kuralı
Basit başla. Spekülatif soyutlama yasak.
Rule of Three: aynı kod 3. kez tekrarlanmadan ortak yapıya çıkarma.
