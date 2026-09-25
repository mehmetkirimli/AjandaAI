# 0015 - Offset tabanlı sayfalama
**Durum:** Kabul edildi
**Tarih:** 2026-09-25

## Bağlam
Liste endpoint'leri sınırsız sayıda kayıt dönebilir; sayfalama gerekti.

## Karar
- Tüm liste endpoint'leri `PagedResult<T>` (Items, TotalCount, Page, PageSize, TotalPages,
  HasPrevious, HasNext) döner.
- Sayfalama offset tabanlıdır: `Skip(filter.Skip).Take(filter.PageSize)`, `CountAsync` ile toplam sayı.
- Varsayılan sayfa boyutu 20, maksimum 100.
- Geçersiz sayfa parametreleri sessizce sınıra çekilir, hata dönülmez.
- Sıralama deterministiktir (ör. `CreatedAt` desc, ardından `Id` desc).
- Filtre parametreleri `[FromQuery]` ile alınır ve opsiyoneldir.

## Gerekçe
Offset (cursor yerine):
- Kullanıcı arayüzünde sayfa numaralarıyla gezinme isteniyor ("sayfa 3'e git"). Cursor
  tabanlı sayfalama bunu desteklemez, yalnızca ileri/geri hareket sağlar.
- Cursor'ın avantajı çok derin sayfalarda ortaya çıkar. Bu uygulamanın öngörülen veri
  hacminde offset yeterli performansı verir.
- Basit başla ilkesi (ADR 0011): cursor gerçek bir performans sorunu doğduğunda eklenebilir.

Geçersiz parametrelerin sessizce sınıra çekilmesi:
- `page=0` veya `pageSize=-5` gibi değerler kullanıcı hatasıdır ve niyeti bellidir. Hata
  dönmek yerine en yakın geçerli değere çekmek daha kullanışlıdır.
- `pageSize` üst sınırı (100) zorunludur: sınırsız bırakılırsa `?pageSize=999999` ile tüm
  tablo çekilip sunucu yorulabilir.

## Sonuçlar
- İstemci doğrudan istediği sayfaya atlayabilir; toplam sayfa sayısı bilinir.
- Kısıt: her istek ek bir COUNT sorgusu çalıştırır.
- Kısıt: derin sayfalarda (OFFSET büyükse) sorgu yavaşlar.
- Kısıt: sayfalama sırasında kayıt eklenir/silinirse kayma veya tekrar görülebilir; bu
  yüzden her sıralamaya ikincil olarak `Id` eklenmiştir.
- Kısıt: istemci hatalı parametre gönderdiğini fark etmeyebilir.
