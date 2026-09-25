# 0001 - Katmanlı mimari ve dikey dilim organizasyonu
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Solution birden fazla projeye bölünmüş (Domain, Application, Infrastructure, Api, Tests)
ve geliştirme birden fazla agent tarafından paralel yürütülüyor. Hem katmanlar arası
bağımlılık yönünün hem de katman içindeki klasör düzeninin belirlenmesi gerekti.

## Karar
- Dört katman: Domain → (hiçbir şey), Application → Domain, Infrastructure → Application,
  Api → Application + Infrastructure. Ters yönde referans yasak.
- Katman içinde kod **kaynak bazlı** (dikey dilim) organize edilir:
  `Application/{Entity çoğulu}/` altında repository interface'i, servis, modül, Dtos/ ve
  Validators/ birlikte durur. `Application/Services/`, `Application/DTOs/` gibi katman
  bazlı klasörler açılmaz.
- İstisna: `Infrastructure/Repositories/` katman bazlıdır.
- Kaynaklar arası bağımlılık tek yönlüdür ve FK yönünü izler
  (Activity → Category/User, Reminder → Activity); bir kaynak kendisine FK veren kaynağı tanımaz.

## Gerekçe
- Dikey dilim: "Bir agent tek klasörde çalışır, çakışma yüzeyi küçülür." (docs/architecture.md)
- Repositories istisnası: tüm repository implementasyonları `AppDbContext`'e bağlıdır.
- Kaynaklar arası tek yön: ters yön bağımlılık döngü oluşturur.
- Katmanlı mimari, iş mantığını altyapıdan ayırarak teknoloji değişikliklerinin etkisini
  sınırlar. Application katmanı EF Core'u tanımaz; bu yüzden testlerde sahte repository ile
  çalışabilir.
- DDD (aggregate, value object, domain event) tercih edilmedi: bu projenin domain karmaşıklığı
  o yapıyı gerektirmiyor. Katmanlı mimari + dikey dilim yeterli ayrımı sağlıyor.

## Sonuçlar
- Bir kaynağa ait tüm Application kodu tek klasörde; paralel çalışmada dosya çakışması azalır.
- Kaynaklar arası döngüsel bağımlılık yapısal olarak engellenir.
- Kısıt: Category/User servisleri Activity verisine erişemez (ör. "kategorideki aktivite sayısı"
  gibi bir ihtiyaç bu kuralla çatışır).
- Kısıt: Infrastructure'da düzen katman bazlı kaldığı için iki farklı organizasyon mantığı bir arada yaşar.
