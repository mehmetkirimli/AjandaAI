# 0007 - IModule deseni ile DI kaydı
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Her kaynağın servis ve validator kayıtları bir yerde yapılmalı. Birden fazla agent paralel
çalıştığı için tüm kayıtların Program.cs'te veya tek bir DependencyInjection.cs'te
toplanması aynı dosyada çakışmaya yol açar.

## Karar
- Servis kayıtları Program.cs'e veya ortak DependencyInjection.cs'e yazılmaz.
- Her kaynak `Application/{Kaynak}/{Kaynak}Module.cs` dosyasında `IModule`
  (`Application/Common/IModule.cs`) implement eder; servis ve validator kayıtları orada yapılır.
- `Infrastructure/DependencyInjection.cs`, Application assembly'deki tüm IModule'leri
  otomatik toplar (modüllerin parametresiz constructor'ı olmalı).
- Repository kayıtları `Infrastructure/DependencyInjection.cs`'te yapılır.
- Tek implementasyonu olan servisler için interface yazılmaz; somut sınıf kaydedilir.
- Aynı mantık EF configuration için de geçerli: `{Entity}Configuration.cs` +
  `ApplyConfigurationsFromAssembly`.

## Gerekçe
- Kuralın başlığı zaten "ÇAKIŞMA ÖNLEME": paylaşılan dosyaya kayıt yazmak paralel çalışmada
  çakışma yaratır. (docs/conventions.md, docs/database.md)
- Repository kayıtları Infrastructure'da: "Application, Infrastructure'daki repository
  sınıflarını göremez."
- Tek implementasyonlu servise interface yazılmaması: spekülatif soyutlama yasağı (bkz. ADR 0011).
- Reflection ile otomatik toplama: her modülün elle kaydedilmesi de mümkündü, ancak elle kayıt
  ortak bir dosyada (DependencyInjection.cs) yapılırdı ve paralel çalışan agent'lar aynı
  dosyada çakışırdı. Çakışmayı kuralla yasaklamak yerine mimariyle imkansız kılmak tercih edildi.
- Maliyeti: her modülün parametresiz constructor'a sahip olması zorunludur ve kayıt hatası
  derleme zamanında değil çalışma zamanında ortaya çıkar.

## Sonuçlar
- Yeni kaynak eklemek paylaşılan dosyaya dokunmayı gerektirmez (repository kaydı hariç).
- Kısıt: yeni repository kaydı için takım liderine bildirim gerekir.
- Kısıt: modüller reflection ile toplandığı için bir modülün kaydı kodda açıkça görünmez;
  parametresiz constructor zorunludur.
- Kısıt: somut servis sınıfları doğrudan enjekte edildiği için testte mock'lanmaları zorlaşır
  (unit testler sahte repository kullanır).
