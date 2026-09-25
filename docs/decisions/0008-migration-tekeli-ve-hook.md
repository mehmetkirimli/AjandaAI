# 0008 - Migration tekeli ve hook ile zorlama
**Durum:** Kabul edildi
**Tarih:** 2026-09-24

## Bağlam
Birden fazla agent paralel çalışıyor ve her biri şemaya etki eden değişiklik yapabilir.
EF Core migration dosyaları zaman damgalı ve zincirlemedir.

## Karar
- Migration üretme yetkisi yalnızca `db-agent`'a aittir. Diğer agent'lar
  `dotnet ef migrations add`, `dotnet ef migrations remove`, `dotnet ef database update`
  çalıştıramaz; ihtiyaç takım liderine veya db-agent'a bildirilir.
- Kural `.claude/hooks/block-migrations.ps1` PreToolUse hook'u ile (Bash|PowerShell)
  zorlanır: `agent_type` `db-agent` değilse exit 2 ile engeller.
- Uygulanmış migration'lar yeniden adlandırılmaz (ör. `MakeUserEmailIndexCaseInsensitive`).

## Gerekçe
- "Paralel üretilen iki migration, birbirini tanımayan iki dal oluşturur ve
  __EFMigrationsHistory tablosunu bozar." (docs/database.md)
- Hook: yalnızca dokümanda yazan kural kazara ihlal edilebilir; hook bir korkuluktur.
- Yeniden adlandırmama: adı değişen uygulanmış migration __EFMigrationsHistory ile eşleşmez.
- Hook'un lead (ana oturum) dahil herkesi engellemesi bilinçli bir tercihtir; kullanıcı bunu
  onaylamıştır. Migration üretmenin iki yolu kalır: db-agent teammate'i veya kullanıcının
  kendi terminali (hook Claude Code dışındaki komutları görmez).
- Böylece migration üretimi her zaman bilinçli bir eylem olur; bir işin yan etkisi olarak
  kazara gerçekleşmez.

## Sonuçlar
- Migration zinciri tek elden, doğrusal ilerler.
- Kısıt: hook dolaylı çağrıları (script içinden) yakalamaz — kasıtlı ihlale karşı koruma değildir.
- Kısıt: şema değişikliği gereken agent db-agent'ı beklemek zorundadır; darboğaz oluşabilir.
- Kısıt: hook ana oturumu (lead) da engeller; `agent_type` yalnızca db-agent'ta geçer.
- Hook kalıbı yalnızca bir komut segmentinin BAŞINDA arar (`;`, `&&`, `||`, `|` ve satır
  sonuna göre bölünmüş). Heredoc / here-string gövdeleri ve tırnaklı argümanlar veri sayılır
  ve incelenmez; `powershell|pwsh|bash|sh|cmd -c "..."` sarmalayıcılarının içi ise ayrıca
  incelenir. Böylece migration komutlarını düz metin olarak içeren bir dosyayı yazmak
  (`cat > x.md << EOF`, `echo "..." > x`) artık engellenmez. (Önceki sürüm komut metninin
  herhangi bir yerinde kalıp aradığı için bu durumları false positive olarak engelliyordu.)
- Kısıt: hook metin tabanlı bir ayrıştırıcıdır, tam bir kabuk parser'ı değildir. Değişken
  içinden (`$cmd = ...; Invoke-Expression $cmd`), `eval` ile veya bir script dosyası
  üzerinden yapılan çağrılar yakalanmaz.
- Regresyon testi: `.claude/hooks/hook-test.ps1` 17 senaryoyu (engellenmesi gereken migration
  komutları; geçmesi gereken dosyaya metin yazma ve build/test komutları) hook'a sahte JSON
  ile verir, başarısızlıkta exit 1 döner. Hook değiştirildiğinde çalıştırılmalıdır
  (docs/commands.md "Hook Testi").
