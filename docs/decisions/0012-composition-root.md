# 0012 - Composition root ve Api -> Infrastructure referansı
**Durum:** Kabul edildi
**Tarih:** 2026-09-23

## Bağlam
Api projesi Infrastructure'a referans veriyor. Katmanlı mimaride bu, "Api veri erişim
detaylarına bağımlı" gibi görünebilir ve referansın kaldırılması önerilebilir.

## Karar
- Api → Infrastructure referansı korunur; bu **composition root** desenidir.
- Api, Infrastructure'ın iç sınıflarını doğrudan kullanmaz.
- Infrastructure DI kayıtlarını `AddInfrastructure(this IServiceCollection, IConfiguration)`
  extension'ı ile sunar; Program.cs yalnızca `builder.Services.AddInfrastructure(builder.Configuration)` çağırır.

## Gerekçe
- Uygulamanın başladığı yer (Api) tüm implementasyonları DI container'a bağlamak zorundadır;
  Application, Infrastructure'daki repository sınıflarını göremez. (docs/architecture.md,
  docs/conventions.md)
- "Bu kasıtlı bir tasarımdır. Api'nin Infrastructure'a referansı 'eksik' değildir,
  composition root desenidir. Kaldırmayın." (docs/architecture.md, CLAUDE.md)
- Alternatif olarak ayrı bir Composition/Host projesi oluşturulabilirdi. Bu, Api'nin
  Infrastructure'a hiç referans vermemesini sağlardı, ancak beş projelik çözüme altıncı bir
  proje ekleme maliyeti getirirdi. Mevcut ölçekte gereksiz bulundu.

## Sonuçlar
- Tüm DI bağlama işi tek bir extension çağrısında toplanır.
- Kısıt: derleyici Api'nin Infrastructure iç sınıflarını kullanmasını engellemez;
  kural disiplinle (ve review ile) korunur.
