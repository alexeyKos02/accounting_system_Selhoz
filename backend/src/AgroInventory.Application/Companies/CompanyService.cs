using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Common;
using AgroInventory.Application.Security;
using AgroInventory.Domain.Constants;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Companies;

/// <summary>
/// Хозяйства (ТЗ §2, §21): список доступных пользователю, карточка, создание (SystemAdmin),
/// изменение. Изоляция: пользователь видит только хозяйства со своим активным членством.
/// </summary>
public sealed class CompanyService
{
    private const string EntityType = "Company";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyContextService _companyContext;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _clock;

    public CompanyService(
        IApplicationDbContext db, ICurrentUser currentUser, CompanyContextService companyContext,
        IAuditLogger audit, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _companyContext = companyContext;
        _audit = audit;
        _clock = clock;
    }

    /// <summary>Хозяйства, доступные пользователю (ТЗ §15). SystemAdmin видит все активные.</summary>
    public async Task<IReadOnlyList<CompanyListItemDto>> ListAsync(CancellationToken ct = default)
    {
        if (_currentUser.IsSystemAdmin)
        {
            return await _db.Companies
                .Where(c => c.Status == CompanyStatus.Active)
                .OrderBy(c => c.Name)
                .Select(c => new CompanyListItemDto(c.Id, c.Name, AppRole.SystemAdmin, true))
                .ToListAsync(ct);
        }

        return await _db.CompanyMemberships
            .Where(m => m.UserId == _currentUser.UserId
                        && m.Status == MembershipStatus.Active
                        && m.Company.Status == CompanyStatus.Active)
            .OrderBy(m => m.Company.Name)
            .Select(m => new CompanyListItemDto(m.CompanyId, m.Company.Name, m.Role, false))
            .ToListAsync(ct);
    }

    public async Task<CompanyDto> GetAsync(Guid companyId, CancellationToken ct = default)
    {
        await _companyContext.RequireForCompanyAsync(companyId, ct); // доступ к хозяйству (ТЗ §24)

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
                      ?? throw NotFoundException.For("Хозяйство", companyId);
        return ToDto(company);
    }

    /// <summary>Создание хозяйства — только системный администратор (ТЗ §4). Проверка политики — в контроллере.</summary>
    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationException(nameof(request.Name), "Название хозяйства обязательно.");

        var now = _clock.GetUtcNow();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            LegalName = Trim(request.LegalName),
            BinOrInn = Trim(request.BinOrInn),
            Country = (request.Country ?? string.Empty).Trim(),
            Timezone = (request.Timezone ?? string.Empty).Trim(),
            Address = Trim(request.Address),
            Description = Trim(request.Description),
            Status = CompanyStatus.Active,
            CreatedByUserId = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Companies.Add(company);

        _audit.Log(AuditAction.Create, EntityType, company.Id, null, new { company.Name, company.Country });
        await _db.SaveChangesAsync(ct);

        return ToDto(company);
    }

    public async Task<CompanyDto> UpdateAsync(Guid companyId, UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var access = await _companyContext.RequireForCompanyAsync(companyId, ct);
        access.Require(Permissions.CompanyManage); // изменение настроек хозяйства (ТЗ §5)

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
                      ?? throw NotFoundException.For("Хозяйство", companyId);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationException(nameof(request.Name), "Название хозяйства обязательно.");

        var old = new { company.Name, company.LegalName, company.Country, company.Timezone };

        company.Name = name;
        company.LegalName = Trim(request.LegalName);
        company.BinOrInn = Trim(request.BinOrInn);
        company.Country = (request.Country ?? string.Empty).Trim();
        company.Timezone = (request.Timezone ?? string.Empty).Trim();
        company.Address = Trim(request.Address);
        company.Description = Trim(request.Description);
        company.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Update, EntityType, company.Id, old,
            new { company.Name, company.LegalName, company.Country, company.Timezone });
        await _db.SaveChangesAsync(ct);

        return ToDto(company);
    }

    private static CompanyDto ToDto(Company c) => new(
        c.Id, c.Name, c.LegalName, c.BinOrInn, c.Country, c.Timezone, c.Address, c.Description, c.Status);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
