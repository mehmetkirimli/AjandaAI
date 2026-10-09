// ICommonPasswordList implementasyonu: embedded resource olan common-passwords.txt'yi bir kez ayrıştırıp
// büyük/küçük harf duyarsız HashSet'te tutar. Boş satırlar ve '#' yorumları atlanır.

using System.Reflection;
using AjandaAI.Application.Auth;

namespace AjandaAI.Infrastructure.Auth;

public sealed class CommonPasswordList : ICommonPasswordList
{
    private const string ResourceName = "AjandaAI.Infrastructure.Auth.common-passwords.txt";

    private readonly HashSet<string> _passwords;

    public CommonPasswordList()
        : this(ReadResource())
    {
    }

    internal CommonPasswordList(string source)
    {
        _passwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in source.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length > 0 && !line.StartsWith('#'))
                _passwords.Add(line);
        }
    }

    public bool Contains(string password) => _passwords.Contains(password);

    private static string ReadResource()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"{ResourceName} gömülü kaynak olarak bulunamadı.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
