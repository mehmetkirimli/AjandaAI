# 0020 - Agent işlerinde kalite kapısı, review agent ve sıralı çalışma
**Durum:** Kabul edildi
**Tarih:** 2026-10-09

## Bağlam
v2 Authentication (ADR 0018) yedi parçaya bölünüp agent'larla yapıldı (P1 db-agent, P2/P6
catalog-agent, P4 activity-agent, P5 reminder-agent; P3/P7/P8 takım lideri). Kabul kriteri
docs/auth-test-senaryolari.md'deki ~50 AUTH-xx senaryosuydu. İki risk vardı:
- Agent "bitti" der ama build kırık, test kırmızı ya da bir senaryonun testi hiç yazılmamıştır.
- Agent kuralları ve kapsamı aşar; bunu otomatik bir kontrol göremez.

## Karar

### 1. Otomatik kalite kapısı (hook)
- `.claude/hooks/quality-gate.ps1`, `TaskCompleted` ve `SubagentStop` (matcher: `^(catalog|activity|reminder|db)-agent$`)
  hook'larına bağlıdır (`.claude/settings.json`).
- Üç kontrol yapar: `dotnet build` 0 warning / 0 error, `dotnet test` yeşil, ve
  `.claude/hooks/required-scenarios.txt` içindeki her kimliğin `tests/` altında en az bir dosyada geçmesi.
  Biri başarısızsa exit 2 ile tamamlanmayı engeller, eksikleri agent'a geri gönderir.
- Senaryo listesi aşamalar ilerledikçe **birikir**: önceki aşamaların kimlikleri listeden çıkmaz,
  böylece sonraki bir aşamanın eski testleri silmesi de yakalanır.
- Her çalışma `.claude/hooks/quality-gate.log`'a bir satır yazar (zaman, event, agent, sonuç).
- Lider kendi kodunu da aynı script'le kontrol eder (elle çalıştırılır).

### 2. Final review agent
- Tüm aşamalar bittikten sonra **tek** bir review çalışır (her aşama sonunda değil).
- Model: Sonnet veya üstü (ADR 0017: review yargı işidir).
- Salt okunur çalışır; kapsamı `git diff <başlangıç commit'i>`'dir ve **takım liderinin kodu dahildir**.
- Prompt'a **kabul edilmiş kararlar** listesi verilir; bunları tekrar bulgu olarak yazmaz.
- Kapının göremediklerine odaklanır: güvenlik doğruluğu, testlerin senaryoyu gerçekten doğrulayıp
  doğrulamadığı, ADR'den sapma, mimari ve ölü kod, aşamalar arası tutarlılık.
- Çıktı: önem sıralı bulgular (dosya:satır, somut senaryo, öneri). Kullanıcı hangilerinin
  düzeltileceğine karar verir; düzeltilmeyenler ilgili ADR'nin "Bilinen kısıtlar"ına yazılır.

### 3. Sıralı çalışma (paralel değil)
- Aynı çalışma klasöründe ve aynı test veritabanında (`ajandaai_test`) birden fazla agent
  aynı anda çalıştırılmaz. Parçalar sırayla yürür (P4 → P5 → P6 → P7).
- Paralel çalışma gerekirse her agent kendi git worktree'sinde ve kendi test veritabanında çalışır;
  birleştirme lidere aittir.

### 4. Görev tanımı sözleşmesi
Her agent görevi şunları AÇIKÇA içerir:
- Hazır olanlar (kullan, değiştirme) ve referans desen (önceki aşamanın dosyaları).
- Kapsam dışı olanlar.
- Dosya izinleri: hangi paylaşılan dosyaya, hangi amaçla, tek seferlik izin verildiği; izin yoksa
  "DUR ve main'e bildir".
- Test kapsamı (AUTH kimlikleri) ve "içi boş test yazma" uyarısı.
- Rapor formatı: değişen dosyalar, senaryo→test tablosu, kalite kapısından mesaj alındı mı,
  tasarım kararları, belirsizlikler.

## Gerekçe
- **Kapı işe yaradı ve kanıtlandı:** İlk denemede kapı, kapsam eksik olduğu için doğru biçimde
  engelledi. Ancak P2 ve P4'te agent'lar "kapıyı görmedim" dedi; hook'un gerçekten tetiklendiği
  ancak log eklendikten sonra (P5: `SubagentStop reminder-agent GECTI`) kanıtlanabildi. Log olmadan
  "hook çalışıyor" iddiası doğrulanamaz.
- **Kapının sınırı:** Kimliğin bir testte geçmesi testin doğru olduğunu kanıtlamaz; kimliği yoruma
  yazıp hiçbir şey assert etmeyen test kapıyı geçer. Bu boşluğu review kapattı (30+ test okundu).
  İkisi birbirinin yerine değil, birbirini tamamlamak için vardır.
- **Review'un değeri:** Final review kritik/yüksek bulgu çıkarmadı ama 5 orta bulgu buldu; ikisi
  takım liderinin kodundaydı (admin loglarında "kim yaptı" yok, eksik `role` alanı admin'i sessizce
  düşürüyor) ve biri liderin verdiği izinden doğmuştu (Program.cs'te Infrastructure sınıfı, ADR 0012).
  Liderin kodu da review'dan geçmelidir.
- **Tek review (aşama başına değil):** Daha ucuz; ayrıca aşamalar arası tutarsızlık ve ölü kod
  (P5 sonrası artık kullanılmayan sahipliksiz repository metotları) ancak bütüne bakınca görünür.
- **Sıralı çalışma:** Paralel denemesinin önünde üç somut engel vardı: aynı `bin/`/`obj/` klasörüne
  eşzamanlı build (dosya kilidi), aynı test veritabanında birbirinin tablosunu temizleyen
  integration testler (agent'ın sağlam kodu "düzeltmeye" kalkması riski) ve tek senaryo listesiyle
  birbirini engelleyen kapı. Paralelliğin daha önce mümkün olduğu görülmüştü; bu turda hedef işi
  bitirmekti.
- **Görev tanımı sözleşmesi:** Agent'ların kapsamı aştığı her durumun kökü görev tanımındaki bir
  boşluktu. Örnek: `Application/Common/` architecture.md'de "lead'in alanı" diye geçiyordu ama
  conventions.md'deki paylaşılan dosyalar listesinde yoktu; P2 agent'ı oraya izin almadan ekleme
  yaptı. Liste genişletildikten sonra bu tekrar etmedi.

## Sonuçlar
- Her agent bitişi build + tüm testler kadar sürer (~30-60 sn); hook timeout'u 600 sn'dir.
- `required-scenarios.txt` lider tarafından her aşamada güncellenmelidir; unutulursa kapı yeni
  aşamanın senaryolarını aramaz.
- Kapı yalnızca kimliğin varlığını kontrol eder; test kalitesi review'a ve lidere kalır.
- `quality-gate.log` bir çalışma kaydıdır, commit'lenmez (`.gitignore`).
- Hook script'i PowerShell 5.1'de çalışır: native komut stderr'i (`2>&1`) `ErrorActionPreference =
  'Stop'` ile script'i durdurur; script bu yüzden `Continue` kullanır.
- Integration testler çalışan bir Postgres container'ı gerektirir; container kapalıysa kapı
  "test başarısız" der ve sebep kod değil ortamdır.
- Sıralı çalışma paralelden yavaştır. Paralel denenecekse 3. kararın worktree + ayrı test DB koşulu
  önce sağlanmalıdır.
