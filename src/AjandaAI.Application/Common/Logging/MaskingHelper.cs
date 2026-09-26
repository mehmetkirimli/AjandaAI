// Kişisel verileri (email, telefon, adres vb.) loglamadan önce maskeleyen yardımcıdır.
// Hiçbir metod exception fırlatmaz; null/boş girdide boş string döner.
// Her maskelemede en az bir karakter gizlenir; 3 karakterden kısa değerler tamamen yıldızlanır.
// Kural seti için bkz. docs/conventions.md "Log Kuralları".

namespace AjandaAI.Application.Common.Logging;

public static class MaskingHelper
{
    private const char MaskChar = '*';
    private const int MinVisibleLength = 3;

    // per*****45@hot****.com
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var value = email.Trim();
        var at = value.LastIndexOf('@');
        if (at < 0)
            return MaskGeneric(value, 3, 2);

        var local = MaskGeneric(value[..at], 3, 2);
        var domain = value[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        var maskedDomain = dot > 0
            ? MaskGeneric(domain[..dot], 3, 0) + domain[dot..]
            : MaskGeneric(domain, 3, 0);

        return $"{local}@{maskedDomain}";
    }

    // ******4567 — yalnızca son 4 hane açık kalır.
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        return MaskGeneric(phone.Trim(), 0, 4);
    }

    // Uzun metinler (adres vb.) için: "Atatürk Cad. No:5 Kadıköy" -> "Atatürk***"
    public static string MaskAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        var firstWord = address.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return firstWord + "***";
    }

    public static string MaskGeneric(string? value, int visibleStart, int visibleEnd)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length < MinVisibleLength)
            return new string(MaskChar, value.Length);

        var start = Math.Max(0, visibleStart);
        var end = Math.Max(0, visibleEnd);

        // Açık kalan kısım değerin tamamını kapsamasın: önce sondan, sonra baştan kırp.
        while (start + end >= value.Length)
        {
            if (end > 0) end--;
            else start--;
        }

        return value[..start]
            + new string(MaskChar, value.Length - start - end)
            + value[(value.Length - end)..];
    }
}
