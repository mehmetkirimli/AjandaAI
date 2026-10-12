# 0023 - Admin ekranları ve kategori sıralaması
**Durum:** Kabul edildi (uygulandı: 2026-10-12)
**Tarih:** 2026-10-12

## Bağlam
F4'te Admin rolü için web ekranları yapılacak: kullanıcılar, kategoriler, aktivite moderasyonu.
Backend uçları P7'de var (`/api/admin/users`, `/api/categories`, `/api/admin/activities`).
Ürün sahibi kategorilerin **sürükle-bırak ile sıralanmasını** istedi (seçenek B); bugün kategoriler
Id sırasıyla listeleniyor ve sırayı saklayan alan yok.

## Karar
### 1. Veri modeli
- `categories.SortOrder` (int, zorunlu, index). Migration mevcut kayıtları Id sırasına göre
  1, 2, 3, ... olarak numaralar.
- Sıra **herkes için ortaktır** (kategoriler ortak lookup'tır); yalnızca Admin değiştirir.

### 2. Sıralama ucu: `PUT /api/categories/order`
- Gövde: `{ "categoryIds": [5, 2, 9, ...] }` — **tüm** kategorilerin (aktif + pasif) Id'leri, yeni sırayla.
- Liste mevcut kategori kümesiyle birebir aynı değilse (eksik, fazla, bilinmeyen veya tekrarlı Id) **400**,
  hiçbir şey değişmez. Geçerliyse `SortOrder = sıra (1'den)` tek transaction'da yazılır. Yanıt 204.
- Yalnızca Admin (AUTH-54 ilkesi); User 403, token'sız 401.
- Eşzamanlı iki sıralama: son kaydeden kazanır (kilit yok; admin sayısı az).

### 3. Diğer kurallar
- Yeni kategori **en sona** eklenir (`max(SortOrder) + 1`).
- Pasife alınan kategori sırasını korur; listelerde görünmez, geri açılınca eski yerinde çıkar.
- `GET /api/categories` varsayılan olarak `SortOrder`'a göre sıralar; yanıtta `sortOrder` alanı bulunur.

### 4. Arayüz
- Sidebar'da yalnızca Admin'e görünen **Yönetim** bölümü: Kullanıcılar, Kategoriler, Aktiviteler.
  User rolü bu sayfalara URL ile girerse ana sayfaya yönlendirilir (backend zaten 403 verir).
- **Kullanıcılar:** tablo + arama; ekle (admin'in eklediği doğrulanmış sayılır), rol değiştir, pasife al.
- **Kategoriler:** sürükle-bırak liste (@dnd-kit); bırakınca sıra hemen kaydedilir. Hata olursa liste eski
  sırasına döner ve "Sıralama kaydedilemedi" bildirimi çıkar. Ekle / yeniden adlandır / pasife al.
- **Aktiviteler:** tüm kullanıcıların aktiviteleri tablo + filtre; pasife alma (moderasyon).
- Kategori sırası geçerli olduğu yerler: aktivite formundaki kategori seçici, takvim/pano filtreleri,
  admin kategori listesi. Pano kartları saate göre sıralanmaya devam eder.

## Gerekçe
- **Tam liste gönderimi:** "şunu şunun önüne taşı" tarzı göreli işlemlere göre basit ve doğrulanabilir;
  yarım/çakışan sıra oluşamaz. Kategori sayısı küçük olduğundan yük sorun değil.
- **Kilitsiz:** optimistic concurrency az sayıda admin için gereksiz karmaşıklık.

## Sonuçlar
- Kabul testleri: [docs/admin-test-senaryolari.md](../admin-test-senaryolari.md) (`ADMIN-xx`).
