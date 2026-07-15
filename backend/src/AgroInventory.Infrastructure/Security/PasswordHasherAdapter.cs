using AgroInventory.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AgroInventory.Infrastructure.Security;

/// <summary>
/// Хеширование паролей поверх Identity PasswordHasher (PBKDF2 с солью, ТЗ §1).
/// Пользователь как параметр хэшеру не нужен — используем фиктивный object.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Dummy = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(Dummy, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
