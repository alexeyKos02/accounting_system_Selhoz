namespace AgroInventory.Api.Security;

/// <summary>
/// Помечает endpoint как работающий в контексте конкретного хозяйства (ТЗ §15, §24).
/// Перед действием <see cref="CompanyAccessFilter"/> проверяет доступ пользователя к выбранному
/// хозяйству (заголовок X-Company-Id) и, если задано, наличие права <see cref="Permission"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireCompanyAttribute : Attribute
{
    public RequireCompanyAttribute(string? permission = null) => Permission = permission;

    /// <summary>Требуемое право (Domain.Constants.Permissions). NULL — достаточно доступа к хозяйству.</summary>
    public string? Permission { get; }
}
