using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Компания / хозяйство (ТЗ §2). Таблица `companies`. «Компания» и «хозяйство» — один уровень;
/// отдельной сущности farms нет. Общий режим строится по всем компаниям, доступным пользователю.
/// </summary>
public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }

    /// <summary>БИН/ИНН (Казахстан/РФ). Необязателен.</summary>
    public string? BinOrInn { get; set; }

    public string Country { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CompanyMembership> Memberships { get; set; } = new List<CompanyMembership>();
}
