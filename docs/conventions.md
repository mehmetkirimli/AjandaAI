# Conventions

## Dosya Başı Comment
Her .cs dosyasının en üstünde, o dosyanın ne işe yaradığını anlatan
maksimum 4 satırlık bir comment bloğu olur.

## İsimlendirme
- Interface: I ile başlar (IActivityService)
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

DependencyInjection.cs assembly'deki tüm IModule'leri otomatik toplar.
Bu dosyaya sadece takım lideri dokunur.

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
