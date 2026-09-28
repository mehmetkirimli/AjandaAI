# Auth Test Senaryoları (v2)

Kararlar ve gerekçeleri: [ADR 0018](decisions/0018-authentication.md).

Bu liste auth işinin **kabul kriteridir**. Review agent ve TaskCompleted kalite kapısı
bu listeye göre kontrol eder: bir senaryonun testi yoksa iş tamamlanmış sayılmaz.

Her senaryonun bir kimliği vardır (`AUTH-xx`). Test adında veya yorumunda bu kimlik geçer,
böylece hangi senaryonun hangi testle karşılandığı aranabilir.

## Kayıt (register)
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-01 | Yeni e-posta ile kayıt | 202; kullanıcı `EmailConfirmedAt = null` ile oluşur; doğrulama maili gönderilir |
| AUTH-02 | Body'de `"role": "Admin"` ile kayıt | Kullanıcı `User` rolüyle oluşur |
| AUTH-03 | Doğrulanmamış bir e-posta ile tekrar kayıt | Eski kayıt hard delete edilir, yenisi oluşur; eski doğrulama linki geçersizdir |
| AUTH-04 | Doğrulanmış ve aktif bir e-posta ile kayıt | 202 ve AUTH-01 ile **aynı gövde**; yeni kayıt oluşmaz; "zaten hesabınız var" maili gider |
| AUTH-05 | Doğrulanmış ama pasife alınmış bir e-posta ile kayıt | AUTH-04 ile aynı |
| AUTH-06 | E-posta büyük/küçük harf farkıyla tekrar kayıt (`Ali@x.com` / `ali@x.com`) | Aynı e-posta sayılır (`ix_users_email_lower`) |

## E-posta doğrulama
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-10 | Geçerli link | `EmailConfirmedAt` dolar |
| AUTH-11 | Aynı link ikinci kez | Reddedilir (tek kullanımlık) |
| AUTH-12 | 12 saati geçmiş link | Reddedilir |
| AUTH-13 | Doğrulama token'ı DB'de | Düz metin değil, SHA-256 hash olarak durur |
| AUTH-14 | Pasife alınmış kullanıcı doğrulama linkine tıklar | `IsActive` değişmez; hesap açılmaz |
| AUTH-15 | Production ortamında DI | `LogEmailSender` kayıtlı DEĞİLDİR |

## Login
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-20 | Doğru bilgiler, doğrulanmış kullanıcı | Access token ve refresh token döner |
| AUTH-21 | Yanlış şifre | 401, "E-posta veya şifre hatalı." |
| AUTH-22 | Kayıtlı olmayan e-posta | AUTH-21 ile **aynı gövde** |
| AUTH-23 | Kayıtlı olmayan e-posta | Şifre hash doğrulaması yine çalışır (dummy hash) |
| AUTH-24 | Doğrulanmamış kullanıcı | Login olamaz |
| AUTH-25 | Pasife alınmış kullanıcı | Login olamaz |
| AUTH-26 | Eski algoritmayla hash'lenmiş şifre ile başarılı login | Şifre yeniden hash'lenir (`SuccessRehashNeeded`) |
| AUTH-27 | Aynı IP'den sınırı aşan login / register denemesi | 429 Too Many Requests |

## Token
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-30 | Access token süresi | 15 dakika |
| AUTH-31 | Süresi dolmuş access token ile istek | 401 |
| AUTH-32 | İmzası bozulmuş / başka anahtarla imzalanmış token | 401 |
| AUTH-33 | Refresh | Yeni access + yeni refresh token döner; eski refresh token `IsRevoked` olur |
| AUTH-34 | Revoke edilmiş refresh token tekrar kullanılır | Reddedilir **ve** kullanıcının tüm refresh token'ları revoke edilir |
| AUTH-35 | 30 günü geçmiş refresh token | Reddedilir |
| AUTH-36 | Refresh token DB'de | Düz metin değil, SHA-256 hash olarak durur |
| AUTH-37 | Logout | Refresh token revoke edilir; sonraki refresh reddedilir |
| AUTH-38 | Refresh sırasında rol DB'de değişmiş | Yeni access token güncel rolü taşır (token'daki eski rol değil) |
| AUTH-39 | Rol Admin → User düşürülür | Kullanıcının tüm refresh token'ları revoke edilir |

## Sahiplik (IDOR)
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-40 | Token olmadan korumalı endpoint | 401 |
| AUTH-41 | Kullanıcı A, B'nin aktivitesini GET eder | 404; gövde gerçek 404 ile byte byte aynı |
| AUTH-42 | Kullanıcı A, B'nin aktivitesini PUT / DELETE eder | 404; B'nin kaydı değişmez |
| AUTH-43 | AUTH-41 senaryosu | "Yetkisiz erişim denemesi" Warning logu yazılır (`UserId`, `ActivityId`) |
| AUTH-44 | Olmayan bir Id | 404; Warning logu **yazılmaz** |
| AUTH-45 | Kullanıcı A, B'nin aktivitesine hatırlatma ekler | Reddedilir (ActivityId sahipliği) |
| AUTH-46 | Liste endpoint'leri | Yalnızca kendi kayıtları döner |
| AUTH-47 | Aktivite oluştururken body'de başka bir `UserId` | Yok sayılır; sahip token'daki kullanıcıdır |

## Admin
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-50 | User rolüyle `/api/admin/...` | 403 |
| AUTH-51 | Admin, aktivite üstverisini okur | Title, Description, Note alanları yanıtta **yoktur** |
| AUTH-52 | Admin, bir aktiviteyi pasife alır | `IsActive = false` |
| AUTH-53 | Admin, kullanıcı endpoint'inden (`/api/activities/{id}`) başkasının kaydına erişir | 404; admin istisnası yoktur |
| AUTH-54 | Kategori yazma işlemleri User rolüyle | 403 |

## Şifre kuralları
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-60 | 9 karakter | Reddedilir |
| AUTH-61 | 10 karakter, sadece küçük harf (`ajandamsin`) | Kabul edilir (karmaşıklık kuralı yok) |
| AUTH-62 | 129 karakter | Reddedilir |
| AUTH-63 | Yaygın şifre listesinde olan (`Password1!`) | Reddedilir |
| AUTH-64 | E-postanın ilk kısmını içeren şifre | Reddedilir |
| AUTH-65 | Görünen adı içeren şifre | Reddedilir |
| AUTH-66 | Boşluklu Türkçe cümle (`ajandam benim en iyi arkadaşım`) | Kabul edilir |
| AUTH-67 | Başında / sonunda boşluk olan şifre | Olduğu gibi saklanır; boşluksuz hali ile login olunamaz |
| AUTH-68 | 72 byte'tan uzun, ilk 72 byte'ı aynı iki farklı şifre | İkincisiyle login olunamaz |

## Log (ADR 0016)
| Id | Senaryo | Beklenen |
|---|---|---|
| AUTH-70 | Register, login, refresh logları | Şifre, access token, refresh token ve doğrulama token'ı loglarda **yoktur** |
| AUTH-71 | Auth loglarında e-posta | Maskelidir (`MaskingHelper.MaskEmail`) |
