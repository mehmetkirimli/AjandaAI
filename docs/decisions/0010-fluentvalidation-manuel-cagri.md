# 0010 - FluentValidation, otomatik pipeline kullanılmaması
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
DTO girdilerinin ve ilişkisel referansların (CategoryId, UserId, ActivityId) doğrulanması gerekiyor.

## Karar
- Doğrulama FluentValidation ile yapılır; validator'lar `Application/{Kaynak}/Validators/` altındadır.
- Otomatik pipeline kullanılmaz (`AddFluentValidationAutoValidation` yok). Servis
  `IValidator<T>`'yi constructor'dan alır ve `ValidateAsync`'i kendisi çağırır.
- Hata durumunda `ApiResponse<T>.Fail("Doğrulama hatası.", errors)` döner; exception fırlatılmaz.
- Validator kaydı kaynağın kendi Module.cs'inde yapılır.
- İlişkisel doğrulama validator'da `MustAsync` ile yapılır; serviste tekrar kontrol edilmez.

## Gerekçe
- Otomatik pipeline yok: "otomatik pipeline Program.cs'e kayıt gerektirir (paylaşılan dosya,
  çakışma riski). Elle çağırma her kaynağın kendi modülünde kalır." (docs/conventions.md)
- DataAnnotations yerine FluentValidation: kuralların bir kısmı alanlar arası ve asenkron —
  `End > Start` karşılaştırması, `CategoryId`'nin veritabanında var ve aktif olması,
  `Rating`'in yalnızca `Status = Completed` iken verilebilmesi. DataAnnotations bu tür
  kuralları ifade etmekte yetersiz kalır.
- Doğrulama kuralları ayrı dosyalarda durur, DTO'lar temiz kalır.

## Sonuçlar
- Doğrulama akışı serviste açıkça görünür; hata formatı ApiResponse ile tutarlıdır.
- Kısıt: her servis metodu validator çağrısını elle yazar (tekrar eden boilerplate);
  çağrıyı unutmak doğrulamanın atlanmasına yol açar.
- Kısıt: validator'lar repository'ye bağımlı olabildiği için birim testte sahte repository gerekir.
