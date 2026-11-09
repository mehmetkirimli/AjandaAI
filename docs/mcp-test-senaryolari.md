# MCP Test Senaryoları (v2.3)

Kararlar ve gerekçeleri: [ADR 0024](decisions/0024-mcp-sunucusu-ve-kisisel-erisim-anahtari.md).

Bu liste MCP işinin **kabul kriteridir** (ADR 0020): bir senaryonun testi yoksa iş tamamlanmış sayılmaz.
Test adında veya yorumunda kimlik (`MCP-xx`) geçer.

**Durum:** kabul edildi (ürün sahibi, 2026-11-09).

## Anahtar yönetimi (`/api/auth/tokens`, JWT oturumu ile)
| Id | Senaryo | Beklenen |
|---|---|---|
| MCP-01 | Anahtar oluştur (ad: "Laptop") | 201; düz anahtar (`ajd_...`) YALNIZCA bu yanıtta; DB'de SHA-256 özeti + ilk 8 karakter (`prefix`) |
| MCP-02 | Anahtarları listele | Ad, prefix, oluşturma/son kullanım/bitiş tarihleri; düz anahtar ve özet DÖNMEZ |
| MCP-03 | 6. etkin anahtarı oluştur | 400, Türkçe mesaj; iptal edilmiş/süresi dolmuşlar sayılmaz |
| MCP-04 | Başkasının anahtarını iptal etmeye çalış | 404 (varlığı sızdırılmaz) |
| MCP-05 | Bir anahtarla yeni anahtar üretmeye çalış | 401: anahtar yalnızca `/mcp`'de geçer |
| MCP-06 | Ad boş veya 100 karakterden uzun | 400 |
| MCP-07 | Oluştururken süre verilmez | `ExpiresAt` = şimdi + 90 gün |

## Kimlik: `/mcp`
| Id | Senaryo | Beklenen |
|---|---|---|
| MCP-10 | Geçerli anahtarla araç çağrısı | Çalışır; `LastUsedAt` güncellenir |
| MCP-11 | Anahtarsız istek | 401 |
| MCP-12 | Uydurma / biçimi bozuk anahtar | 401 |
| MCP-13 | İptal edilmiş anahtar | 401 |
| MCP-14 | Süresi dolmuş anahtar | 401 |
| MCP-15 | Pasife alınmış kullanıcının anahtarı | 401 |
| MCP-16 | Anahtarla `/api/activities`'e istek | 401 (anahtar normal API'de GEÇMEZ) |
| MCP-17 | Normal JWT ile `/mcp`'ye istek | 401 (`/mcp` yalnızca anahtar kabul eder) |
| MCP-18 | Kullanıcı şifresini değiştirir | Anahtarları geçerli kalır (ADR 0024 §1, bilinçli) |
| MCP-19 | Anahtarlı bir istek loglanır | Log'da anahtar görünmez (maskeleme, ADR 0016) |

## Araçlar
| Id | Senaryo | Beklenen |
|---|---|---|
| MCP-20 | `aktivite_ekle` geçerli girdiyle | Aktivite anahtar sahibine ait oluşur; `GET /api/activities` ile web'de görünür |
| MCP-21 | `aktivite_ekle` bitiş < başlangıç | Araç hatası, Türkçe mesaj (web formuyla aynı); kayıt oluşmaz |
| MCP-22 | `aktivite_ekle` olmayan veya pasif kategori | Araç hatası; kayıt oluşmaz |
| MCP-23 | `aktivite_ekle` girdisine başka bir `userId` eklenir | Yok sayılır; aktivite anahtar sahibine yazılır |
| MCP-24 | `haftayi_listele` | Yalnızca anahtar sahibinin, etkin aktiviteleri |
| MCP-25 | `haftayi_listele` hafta sınırı | Kullanıcının saat dilimine göre (İstanbul, Pazar 23:30 aktivitesi o haftada) |
| MCP-26 | `bos_zaman_bul` (gün içi pencere + en az süre) | Yalnızca pencere içindeki, en az süreyi karşılayan boşluklar |
| MCP-27 | `bos_zaman_bul` gece yarısını aşan aktivite | İki günün boşluğu da doğru hesaplanır |
| MCP-28 | `bos_zaman_bul` aralık 31 günden uzun | Araç hatası |
| MCP-29 | `kategorileri_listele` | Yalnızca etkin kategoriler, admin sırasıyla (ADR 0023) |
| MCP-30 | Araç listesi | Tam 5 araç; silme/güncelleme aracı YOK |
| MCP-31 | `seri_ekle` "her gün 23:00, 1 yıl" | Tek çağrıda 365 oluşum, hepsi anahtar sahibine ait ve aynı seride (ADR 0022) |
| MCP-32 | `seri_ekle` 366'dan fazla oluşum | Araç hatası; hiçbir oluşum yazılmaz |
| MCP-33 | Bir anahtarla 1 dakikada 31. araç çağrısı | 429; başka anahtar/kullanıcı etkilenmez |

## Geçmiş tarih kuralı — her yerde (ADR 0024 §2b)
| Id | Senaryo | Beklenen |
|---|---|---|
| PAST-01 | Web: başlangıcı 29 gün önce olan aktivite oluştur | 201 |
| PAST-02 | Web: başlangıcı 31 gün önce olan aktivite oluştur | 400, alanın altında Türkçe mesaj |
| PAST-03 | Güncelle: 40 gün önceki aktivitenin yalnızca puanı/notu değişir | 200 (başlangıç değişmediği için kural uygulanmaz) |
| PAST-04 | Güncelle / takvimde sürükle: başlangıç 31 gün öncesine taşınır | 400 |
| PAST-05 | Seri: ilk oluşum 31 gün önce | 400; hiçbir oluşum yazılmaz |
| PAST-06 | `aktivite_ekle` 31 gün önceye | Araç hatası, aynı Türkçe mesaj |

## Ertelenenler
- Anahtar yeni bir cihazdan ilk kez kullanılınca bildirim → v2.4 (e-posta altyapısı).
