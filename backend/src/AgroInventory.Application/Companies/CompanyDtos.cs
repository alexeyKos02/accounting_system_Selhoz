using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Companies;

// ---------- Хозяйства (ТЗ §2, §21) ----------

/// <summary>Хозяйство в списке доступных (для переключателя, ТЗ §15). MyRole — роль текущего пользователя.</summary>
public sealed record CompanyListItemDto(Guid Id, string Name, AppRole MyRole, bool IsSystemAdmin);

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? BinOrInn,
    string Country,
    string Timezone,
    string? Address,
    string? Description,
    CompanyStatus Status);

public sealed record CreateCompanyRequest(
    string Name,
    string? LegalName,
    string? BinOrInn,
    string Country,
    string Timezone,
    string? Address,
    string? Description);

public sealed record UpdateCompanyRequest(
    string Name,
    string? LegalName,
    string? BinOrInn,
    string Country,
    string Timezone,
    string? Address,
    string? Description);

// ---------- Членства и доступы (ТЗ §3, §6, §21) ----------

public sealed record MemberDto(
    Guid MembershipId,
    Guid UserId,
    string? Email,
    string DisplayName,
    AppRole Role,
    MembershipStatus Status);

public sealed record AddMemberRequest(Guid UserId, AppRole Role);

public sealed record UpdateMemberRequest(AppRole Role, MembershipStatus Status);

public sealed record ScopeItemDto(AccessScopeType ScopeType, Guid? ScopeEntityId);

public sealed record MemberScopesDto(bool HasFullCompanyScope, IReadOnlyList<ScopeItemDto> Scopes);

public sealed record UpdateScopesRequest(IReadOnlyList<ScopeItemDto> Scopes);
