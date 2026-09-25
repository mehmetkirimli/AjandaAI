# 0009 - ApiResponse<T> ve ResultType ile HTTP durum yönetimi
**Durum:** Kabul edildi
**Tarih:** 2026-09-24

## Bağlam
Controller'ların tutarlı bir response yapısı dönmesi ve HTTP status kodunun
tek bir yerden belirlenmesi gerekti.

## Karar
- Tüm controller'lar `ApiResponse<T>` (Success, Data, Message, Errors) döner.
  Ham entity dönmek yasak; her zaman DTO döner.
- `ApiResponse<T>` `[JsonIgnore]` bir `ResultType` taşır. Servisler sonucu
  `Ok / Created / NoContent / Fail / NotFound / Conflict / Error` yardımcılarıyla üretir.
- `Api/Filters/ApiResponseFilter.cs` (IAsyncResultFilter) ResultType'a bakarak HTTP status'u belirler.
- Controller metodları tek satırdır; `ActionResult` ve `[ProducesResponseType]` kullanılmaz,
  dönüş tipi `Task<ApiResponse<T>>`'dir.
- Kayıt bulunamadığında null dönülmez, NotFound döner.

## Gerekçe
- Status kodu body'ye yazılmaz: "tek kaynak ilkesi: status yalnızca HTTP katmanında yaşar."
  (docs/conventions.md)
- `[ProducesResponseType]` yok: Swagger dokümantasyonu gerekirse status kodları tek tek
  attribute ile değil, merkezi bir IOperationFilter ile eklenecek.
- Envelope (sarmalayıcı) yapısı: istemci her yanıtta aynı şekli görür; başarı ve hata
  durumları tek bir parse mantığıyla işlenir.
- Ham entity dönmek yasak: entity'ler veritabanı şemasını yansıtır. Doğrudan dönülürse şema
  değişikliği API sözleşmesini kırar; ayrıca istemciye gösterilmemesi gereken alanlar sızabilir.
- ActionResult yerine filter: controller metodları tek satıra iner, HTTP durum kodu hesaplama
  sorumluluğu tek yerde toplanır. Her controller'ın kendi `NotFound()`/`BadRequest()`
  çağrılarını yazması tekrar ve tutarsızlık üretiyordu.

## Sonuçlar
- Servisler HTTP'den habersizdir; status eşlemesi tek dosyadadır.
- Controller'lar ince ve tekdüze kalır.
- Kısıt: Swagger şu an response status kodlarını göstermez.
- Kısıt: 204 NoContent gövde taşıyamadığı için filter özel davranış uygular.
