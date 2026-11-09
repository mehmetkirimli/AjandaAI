# MCP Kurulumu: Claude'u AjandaAI'ye bağlamak

Kararlar: [ADR 0024](decisions/0024-mcp-sunucusu-ve-kisisel-erisim-anahtari.md). Senaryolar: [mcp-test-senaryolari.md](mcp-test-senaryolari.md).

## 1. Anahtar üret
Profil sayfası → "Claude için anahtar oluştur" → bir ad ver (örn. "Laptop - Claude Code").
Anahtar (`ajd_...`) **yalnızca bir kez** gösterilir; kopyala. Kaybedersen iptal edip yenisini üret.

> Profil ekranı hazır olana kadar: Swagger'da `POST /api/auth/tokens` (normal girişle "Authorize") aynı işi yapar.

## 2. Claude Code'a ekle
Backend çalışırken (`dotnet run --project backend/src/AjandaAI.Api --launch-profile http`):

```
claude mcp add --transport http --scope local ajandaai http://localhost:5250/mcp --header "Authorization: Bearer ajd_BURAYA_ANAHTAR"
```

- `--scope local`: ayar yalnızca senin makinende (`~/.claude.json`) durur. Anahtar repoya GİRMEZ —
  `--scope project` KULLANMA (`.mcp.json` commit'lenir, anahtar sızar).
- Kontrol: `claude mcp list` → `ajandaai ... √ Connected`.
- Dene: "Perşembe 19:00'a sinema ekle", "Bu hafta neler var?", "Cuma akşamı 2 saatlik boşluğum var mı?"

## 3. Claude Desktop (henüz denenmedi)
Şirket hesaplarında yönetici özel bağlantıları kapatmış olabilir. Açıksa yerel bir köprüyle (`mcp-remote`)
aynı adres ve başlık verilir; v2.3b'de doğrulanıp buraya yazılacak.

## Araçlar
| Araç | Ne yapar |
|---|---|
| `aktivite_ekle` | Tek aktivite ekler |
| `seri_ekle` | Tekrarlayan seri (en fazla 366 oluşum) |
| `haftayi_listele` | Bir haftanın aktiviteleri |
| `bos_zaman_bul` | Boş zaman aralıkları (en fazla 31 gün) |
| `kategorileri_listele` | Etkin kategoriler |

Sınırlar: anahtar başına dakikada 30 çağrı; başlangıç en fazla 30 gün geçmişte. Silme/güncelleme yok.

## Güvenlik
- Anahtar yalnızca `/mcp`'de geçer; normal API'ye giremez.
- Sızdığını düşünürsen profilden iptal et — anında geçersiz olur. Süresi 90 gün.
- Claude'un okuduğu aktiviteler sohbetin parçası olur (Anthropic modeline gider); şirket hesabıyla
  kullanıyorsan şirket hesabının sohbetlerinden geçer.

## v2.3b: hazır MCP'ler (planlandı)
MongoDB (log analizi, salt okunur kullanıcı), hava durumu, harita (ücretsiz seçenekle başla), `server-memory`
(Claude Desktop'ta planlama tercihleri). Kurulum adımları seçildikçe buraya eklenecek.
