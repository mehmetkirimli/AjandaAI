// Gerçek IPasswordService'i sarıp çağrıları sayan test yardımcısı.
// AUTH-23 (kullanıcı yokken dummy doğrulama) ve AUTH-04 (kayıtlı e-postada da hash) timing eşitlemesini
// süre ölçmeden, "hash/doğrulama gerçekten çalıştı mı" diye sayarak doğrular.

using AjandaAI.Application.Auth;

namespace AjandaAI.IntegrationTests.Auth;

public sealed class CountingPasswordService : IPasswordService
{
    private readonly IPasswordService _inner;
    private int _hash;
    private int _verify;
    private int _dummy;

    public CountingPasswordService(IPasswordService inner)
    {
        _inner = inner;
    }

    public int HashCount => Volatile.Read(ref _hash);

    public int VerifyCount => Volatile.Read(ref _verify);

    public int DummyVerifyCount => Volatile.Read(ref _dummy);

    public string Hash(string password)
    {
        Interlocked.Increment(ref _hash);
        return _inner.Hash(password);
    }

    public PasswordVerifyResult Verify(string passwordHash, string password)
    {
        Interlocked.Increment(ref _verify);
        return _inner.Verify(passwordHash, password);
    }

    public void VerifyDummy(string password)
    {
        Interlocked.Increment(ref _dummy);
        _inner.VerifyDummy(password);
    }
}
