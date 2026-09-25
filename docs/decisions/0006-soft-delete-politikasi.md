# 0006 - Soft delete politikası
**Durum:** Kabul edildi
**Tarih:** 2026-09-24

## Bağlam
Entity'ler arasında FK ilişkileri var (Activity → Category, User; Reminder → Activity).
Silme işleminin nasıl yapılacağına karar verilmesi gerekti.

## Karar
- Başka bir kaydın FK verdiği entity'ler SOFT delete edilir (`IsActive = false`).
- Hiçbir kaydın FK vermediği entity'ler HARD delete edilir.
- Mevcut: Category, User, Activity → soft; Reminder → hard.
- Soft delete edilen entity'ye FK veren validator'lar `ExistsAsync` değil `IsActiveAsync`
  kullanır; pasif kayda yeni bağlantı kurulamaz.
- DELETE başarılı olduğunda (soft/hard) `Ok` döner.

(docs/conventions.md "Silme Politikası")

## Gerekçe
- Soft delete'in amacı geçmişi korumaktır. Bir kayda FK veren başka kayıtlar varsa, o kayıt
  silindiğinde bağlı kayıtlar sahipsiz kalır veya cascade ile birlikte silinir. Her iki durum
  da veri kaybıdır.
- Hiçbir kaydın FK vermediği entity'de korunacak bağ yoktur. Soft delete uygulanırsa her
  sorguya `IsActive` filtresi eklenir; bu filtreyi unutan bir kod silinmiş sayılan kaydı işler.
  Reminder örneğinde bu, silinmiş bir hatırlatmanın bildirim göndermesi demektir.
- Ölçüt böylece nesnel hale gelir: karar FK yönüne bakılarak verilir, tartışmaya gerek kalmaz.

## Sonuçlar
- Pasif kayıtlara bağlı mevcut kayıtlar bozulmaz.
- Kısıt: liste sorguları `IsActive` filtresi uygulamak zorundadır.
- Kısıt: soft delete edilen entity'lere FK veren tüm validator'lar `ExistsAsync` değil
  `IsActiveAsync` kullanmak zorundadır.
- Kısıt: yeni bir entity eklenirken silme politikası FK yönüne bakılarak belirlenir; yeni
  entity bir tabloya FK verdiğinde o tablonun politikası hard'dan soft'a geçmelidir.
- Kısıt: soft delete edilen veriler veritabanında kalmaya devam eder.
