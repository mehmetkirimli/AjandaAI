# Architecture Decision Records (ADR)

## ADR nedir?
Architecture Decision Record, projede alınmış önemli bir teknik kararı
**bağlamı, gerekçesi ve sonuçlarıyla** birlikte kaydeden kısa bir belgedir.
Kod "ne yapıldığını" gösterir; ADR "neden böyle yapıldığını" anlatır.

Yeni bir oturum veya yeni bir geliştirici "bu neden böyle?" diye sorduğunda
cevap burada olmalıdır. Bir kuralı değiştirmeden önce ilgili ADR okunur.

## Dosya adlandırma
`NNNN-kisa-baslik.md` — dört haneli sıra numarası + kebab-case başlık.
Örnek: `0003-postgresql-snake-case.md`. Numaralar yeniden kullanılmaz.
`0000-sablon.md` şablondur, karar değildir.

## Yeni ADR nasıl eklenir
1. `0000-sablon.md` dosyasını bir sonraki boş numarayla kopyala.
2. Bağlam, Karar, Gerekçe, Sonuçlar bölümlerini doldur.
3. Gerekçe bilinmiyorsa uydurma; şunu yaz:
   `> GEREKÇE EKSİK - kullanıcı tamamlayacak`
4. Aşağıdaki listeye ekle.
5. Kabul edilmiş bir ADR'nin kararı sonradan değişirse eski dosya silinmez:
   durumu `Değiştirildi` yapılır ve yeni ADR'ye link verilir.

## Durum değerleri
| Durum        | Anlamı                                              |
|--------------|-----------------------------------------------------|
| Kabul edildi | Karar geçerli ve uygulanıyor.                       |
| Değiştirildi | Yerini başka bir ADR aldı (link verilir).           |
| Reddedildi   | Değerlendirildi ama uygulanmadı; neden kaydı kalır. |

## Kayıtlar
- [0001 - Katmanlı mimari ve dikey dilim organizasyonu](0001-katmanli-mimari-ve-dikey-dilim.md)
- [0002 - Enum'ların veritabanında string olarak saklanması](0002-enum-string-saklama.md)
- [0003 - PostgreSQL snake_case isimlendirme](0003-postgresql-snake-case.md)
- [0004 - DateTimeOffset kullanımı](0004-datetimeoffset-kullanimi.md)
- [0005 - int Id kullanımı](0005-int-id-kullanimi.md)
- [0006 - Soft delete politikası](0006-soft-delete-politikasi.md)
- [0007 - IModule deseni ile DI kaydı](0007-imodule-deseni.md)
- [0008 - Migration tekeli ve hook ile zorlama](0008-migration-tekeli-ve-hook.md)
- [0009 - ApiResponse<T> ve ResultType ile HTTP durum yönetimi](0009-apiresponse-ve-resulttype.md)
- [0010 - FluentValidation, otomatik pipeline kullanılmaması](0010-fluentvalidation-manuel-cagri.md)
- [0011 - Spekülatif soyutlamadan kaçınma (Rule of Three)](0011-rule-of-three.md)
- [0012 - Composition root ve Api -> Infrastructure referansı](0012-composition-root.md)
- [0013 - Katman sınırında exception çevirimi](0013-katman-sinirinda-exception-cevirimi.md)
- [0014 - Ortam ayrımı (Development/Test/Production)](0014-ortam-ayrimi.md)
- [0015 - Offset tabanlı sayfalama](0015-offset-tabanli-sayfalama.md)
- [0016 - Log altyapısı ve hassas veri maskeleme](0016-log-altyapisi-ve-maskeleme.md)
- [0017 - Agent'lara model ataması](0017-agent-model-atamasi.md)
