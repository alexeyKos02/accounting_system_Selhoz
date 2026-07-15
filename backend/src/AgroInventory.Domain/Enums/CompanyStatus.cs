namespace AgroInventory.Domain.Enums;

/// <summary>Статус компании/хозяйства (ТЗ §2). Явного перечня в ТЗ нет — активна/архив.</summary>
public enum CompanyStatus
{
    Active = 0,
    Archived = 1,
}
