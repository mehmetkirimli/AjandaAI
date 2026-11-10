// Asistan araçlarının sabit anahtar adları (ADR 0025 §3). Ad değişmez: ayardaki aç/kapa listesi,
// loglar ve paneldeki "hava durumuna baktı" rozetleri bu adlara bağlıdır.
// Yönetici araçları (log/veritabanı) ve hafıza v2.4b'dedir; burada yoktur.

namespace AjandaAI.Application.Assistant;

public static class AssistantToolNames
{
    public const string HaftayiListele = "haftayi_listele";
    public const string BosZamanBul = "bos_zaman_bul";
    public const string KategorileriListele = "kategorileri_listele";
    public const string TatilleriListele = "tatilleri_listele";
    public const string HavaDurumu = "hava_durumu";
    public const string YerAra = "yer_ara";
    public const string AktiviteOner = "aktivite_oner";
    public const string SeriOner = "seri_oner";

    public static readonly IReadOnlyList<string> All =
    [
        HaftayiListele, BosZamanBul, KategorileriListele, TatilleriListele, HavaDurumu, YerAra, AktiviteOner, SeriOner
    ];
}
