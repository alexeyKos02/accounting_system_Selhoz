namespace AgroInventory.Application.Abstractions;

/// <summary>
/// Безопасное хеширование паролей (ТЗ §1). Реализация в Infrastructure поверх
/// Microsoft.AspNetCore.Identity PasswordHasher (PBKDF2 с солью).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Проверяет пароль против хэша.</summary>
    bool Verify(string hash, string password);
}
