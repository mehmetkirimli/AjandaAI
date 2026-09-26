# 0017 - Agent'lara model ataması
**Durum:** Kabul edildi
**Tarih:** 2026-09-26

## Bağlam
Agent Teams'te her teammate'e farklı model atanabiliyor. Token maliyeti ile iş kalitesi
arasında denge kurmak için hangi işe hangi modelin uygun olduğunu bilmek gerekiyordu.

## Karar
Teammate'lere iş tipine göre model atanır:
- Yargı, tasarım kararı veya belirsizlik içeren işler: Sonnet veya üstü.
- Kuralları net tanımlanmış, mekanik, tekrarlayan işler: Haiku.

## Gerekçe
Serilog turunda (bkz. ADR 0016) iki teammate farklı modellerle çalıştırıldı ve sonuçlar
karşılaştırıldı.

Sonnet (`infra`, altyapı kurulumu):
- Paket sürüm çakışmasını kendisi fark edip çözdü (net8 uyumlu sürümleri pinledi).
- Gereksiz bağımlılık eklemekten kaçındı.
- Paylaşılan dosya kuralına takılınca DURDU ve takım liderinden izin istedi.
- Gereksiz değişiklik yapmadı (ExceptionHandlingMiddleware zaten uygundu, dokunmadı).

Haiku (`events`, servislere log ekleme):
- Structured logging formatına harfiyen uydu; build ve testler temizdi.
- ANCAK: hassas veri (email) logladı. "Hassas veri loglama" talimatı hangi alanın
  hassas olduğuna dair yargı gerektirdiği için uygulanamadı.
- Faydasız Warning logları yazdı (hangi alanın hata verdiği yoktu).
- Kapsam dışı loglar ekledi (hatırlatma güncelleme, aktivite listesi).
- Kendi raporunda tutarsız sayılar verdi (test dağılımı toplamı 66 etmiyordu).

Sonuç: Haiku kurala uymada başarılı, kuralın boşluğunu doldurmada yetersiz.
Sonnet boşlukları makul biçimde doldurur.

## Sonuçlar
- Haiku'ya iş verilirken talimatlar boşluk bırakmamalıdır. "Hassas veri loglama" yerine
  "Email, telefon, adres alanlarını loglama" yazılmalıdır.
- Haiku çıktısı gözden geçirilmelidir. Model ataması denetimi ortadan kaldırmaz,
  maliyeti düşürür.
- Model, spawn sırasında Agent aracının `model` parametresiyle verilir ve agent
  tanımındaki (`.claude/agents/*.md`) `model: inherit` değerini ezer.
- Doğrulama yöntemi: her teammate'e sistem prompt'unda gördüğü model ID'si soruldu;
  `infra` `claude-sonnet-5`, `events` `claude-haiku-4-5-20251001` bildirdi. Bu, agent'ın
  kendi beyanıdır; API seviyesinde bağımsız bir doğrulama yapılmamıştır.
- Tek bir karşılaştırmaya dayanır (n=1); farklı iş tiplerinde tekrar değerlendirilmelidir.
