# Asistan Test Senaryoları (v2.4)

Kararlar: [ADR 0025](decisions/0025-uygulama-ici-asistan.md). Kabul kriteridir (ADR 0020): testi olmayan senaryo bitmemiştir.
Test adında/yorumunda kimlik (`ASSIST-xx`) geçer.

**Test yaklaşımı:** testler gerçek Gemini'yi ÇAĞIRMAZ (ücretsiz kota, kararsız çıktı). `IChatClient` sahte bir
uygulamayla değiştirilir; sahte istemci "şu aracı şu girdiyle çağır" der, gerçek araç kodu ve veritabanı çalışır.
Gerçek modelle uçtan uca deneme lider kabulünde elle yapılır.

**Durum:** kabul edildi (ürün sahibi, 2026-11-10). Sınırlar: dakikada 10, günde 200 mesaj.

## Yapılandırma ve erişim
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-01 | Anahtar tanımlı değil, sohbet isteği | 503, "Asistan yapılandırılmadı."; uygulamanın diğer uçları normal çalışır |
| ASSIST-02 | Oturumsuz istek | 401 |
| ASSIST-03 | Kişisel erişim anahtarıyla (`ajd_...`) sohbet ucu | 401 (anahtar yalnızca `/mcp`'de geçer) |

## Araçlar ve sahiplik
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-10 | Model `aktivite_oner` çağırır, kullanıcı onaylar | Aktivite sohbet eden kullanıcıya ait oluşur, web listesinde görünür |
| ASSIST-11 | Model `haftayi_listele` çağırır | Yalnızca o kullanıcının etkin aktiviteleri döner |
| ASSIST-12 | Araç doğrulama hatası (bitiş < başlangıç, 30 günden eski) | Modele Türkçe hata metni döner, kayıt oluşmaz, sohbet çökmez |
| ASSIST-13 | Model araç girdisine başka `userId` koyar | Yok sayılır |
| ASSIST-14 | Kapalı bir araç (ayarda devre dışı) | Modele hiç sunulmaz; çağrılmaya çalışılırsa reddedilir |
| ASSIST-15 | Hava aracı: dış servis hata verir | Araç hatası modele döner, sohbet devam eder |
| ASSIST-16 | Yer arama aynı sorguyla iki kez | İkinci sonuç önbellekten; dış servise saniyede en fazla 1 istek, uygulama User-Agent'ı |
| ASSIST-17 | Tatil aracı aynı yıl için iki kez; dış servis hata verir | İkinci sonuç önbellekten; hata modele Türkçe döner, sohbet devam eder |

## Varsayılan konum (ürün sahibi kararı, 2026-11-11)
Profilde yalnızca şehir/ilçe metni ("Üsküdar, İstanbul"); tam adres tutulmaz (ücretsiz katmanda Google'a gider).
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-60 | Kullanıcı profilinden konumunu girer / değiştirir / siler | `GET/PUT /api/auth/me` ile döner ve kaydedilir; boş bırakılabilir |
| ASSIST-61 | Konum 100 karakterden uzun | 400, Türkçe doğrulama mesajı |
| ASSIST-62 | Konumu olan kullanıcı sohbet eder | Sistem talimatı konumu içerir ("yer belirtilmezse bunu kullan") |
| ASSIST-63 | Konumu olmayan kullanıcı | Sistem talimatında konum yok; model sorar |

## Admin araçları ve kişisel hafıza (ürün sahibi kararı, 2026-11-10)
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-50 | Normal kullanıcının sohbetinde araç listesi | Log ve veritabanı araçları modele hiç sunulmaz |
| ASSIST-51 | Normal kullanıcı adına log/veritabanı aracı çağrılmaya çalışılır | Reddedilir, veri dönmez |
| ASSIST-52 | Admin log/veritabanı aracını kullanır | Salt okunur sonuç döner; yazma/silme sorgusu reddedilir |
| ASSIST-53 | Veritabanı aracı şifre özeti veya token tablosu ister | İzin hatası, veri dönmez |
| ASSIST-54 | Kullanıcı profilinden not ekler, değiştirir, siler | Kendi notları güncellenir; başka kullanıcının notuna erişim 404 |
| ASSIST-55 | Asistan sohbette notları kullanır | Yalnızca o kullanıcının notları sistem bağlamına girer |
| ASSIST-56 | Asistan yeni not önerir | Kaydedilmez; "Hatırlayayım mı?" kartı onaylanınca not oluşur |

## Sınırlar
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-20 | Mesaj 2.000 karakterden uzun | 400 |
| ASSIST-21 | Geçmiş 20 mesajdan fazla | 400 |
| ASSIST-22 | Bir kullanıcı dakikada 11. mesaj | 429; başka kullanıcı etkilenmez |
| ASSIST-23 | Günlük sınır (200 mesaj) aşılır | 429, Türkçe mesaj |
| ASSIST-24 | Model tek yanıtta 8'den fazla araç çağrısına gider | Döngü durur, kullanıcıya Türkçe açıklama |
| ASSIST-25 | Sağlayıcı kota/sunucu hatası (429/5xx) | 503 "Asistan şu an yoğun, biraz sonra dene."; uygulama etkilenmez |

## Gizlilik ve bağlam
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-30 | Sohbet loglanır | Kullanıcı mesajı ve model cevabı metni loglarda YOK; araç adı, süre, token sayısı VAR |
| ASSIST-31 | Sistem talimatı | Kullanıcının saat dilimini ve yerel "bugün"ü içerir |

## Onay akışı (ürün sahibi kararı, 2026-11-10: her ekleme onaya sunulur)
| Id | Senaryo | Beklenen |
|---|---|---|
| ASSIST-40 | Model `aktivite_oner` / `seri_oner` çağırır | Veritabanına HİÇBİR şey yazılmaz; yanıtta yapılandırılmış öneri (`proposals`) döner |
| ASSIST-41 | Öneri, doğrulamadan geçmeyen bir kayıt (30 günden eski, bitiş < başlangıç) | Öneri üretilmez; modele Türkçe hata döner |
| ASSIST-42 | Kullanıcı öneride "Deftere yaz"a basar | Kayıt mevcut uçtan (`POST /api/activities` / `/series`) oluşur, web formuyla aynı doğrulama |
| ASSIST-43 | Kullanıcı "Vazgeç"e basar | Hiçbir kayıt oluşmaz |
