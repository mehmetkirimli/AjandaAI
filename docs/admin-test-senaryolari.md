# Admin ve kategori sırası test senaryoları

Kararlar: [ADR 0023](decisions/0023-admin-ekranlari-ve-kategori-sirasi.md). Kimlik test adında veya yorumunda
geçer (`ADMIN-xx`); kalite kapısı (ADR 0020) bu kimlikleri arar.

## Kategori sırası (backend)
| Id | Senaryo | Beklenen |
|---|---|---|
| ADMIN-01 | Migration sonrası mevcut kategoriler | `SortOrder` Id sırasıyla 1..N |
| ADMIN-02 | `GET /api/categories` | `SortOrder`'a göre sıralı, yanıtta `sortOrder` |
| ADMIN-03 | Admin, tüm Id'leri yeni sırayla `PUT /api/categories/order` | 204; `GET` yeni sırayı döner |
| ADMIN-04 | Listede bir Id eksik | 400; sıra değişmez |
| ADMIN-05 | Listede bilinmeyen Id | 400; sıra değişmez |
| ADMIN-06 | Listede tekrarlı Id | 400; sıra değişmez |
| ADMIN-07 | User rolü sıralama dener | 403; token'sız 401 |
| ADMIN-08 | Yeni kategori oluşturma | en sona eklenir (`max + 1`) |
| ADMIN-09 | Kategori pasife alınır, sonra yeniden aktif edilir | sırası korunur, eski yerinde döner |
| ADMIN-10 | Pasif kategori sıralama listesinde | tam liste pasifleri de içerir; geçerli kabul edilir |
| ADMIN-11 | Admin `GET /api/categories?includeInactive=true`; User aynı parametreyle | Admin: aktif + pasif, SortOrder sırasıyla; User: parametre yok sayılır, yalnızca aktifler |
| ADMIN-12 | Admin pasif kullanıcıyı yeniden aktif eder (`PUT /api/admin/users/{id}` `isActive: true`) | kullanıcı tekrar giriş yapabilir |

## Ekran (web, Playwright kabulü)
| Id | Senaryo | Beklenen |
|---|---|---|
| ADMIN-20 | User rolüyle giriş | Yönetim menüsü yok; `/admin/...` URL'si ana sayfaya döner |
| ADMIN-21 | Admin kategoriyi sürükleyip bırakır, sayfayı yeniler | yeni sıra kalır; aktivite formunda da o sırayla |
| ADMIN-22 | Sıralama kaydı başarısız (API hata) | liste eski sıraya döner, "Sıralama kaydedilemedi" |
| ADMIN-23 | Admin kullanıcı ekler | listede doğrulanmış görünür; o kullanıcı e-posta doğrulamadan giriş yapar |
| ADMIN-24 | Admin kullanıcıyı pasife alır | listede pasif görünür; o kullanıcı giriş yapamaz |
| ADMIN-25 | Admin bir kullanıcının rolünü User'a düşürür | o kullanıcının oturumu kapanır (AUTH-39) |
| ADMIN-26 | Admin aktiviteler ekranında başka kullanıcının aktivitesini pasife alır | listede pasif; sahibinin panosunda görünmez |
