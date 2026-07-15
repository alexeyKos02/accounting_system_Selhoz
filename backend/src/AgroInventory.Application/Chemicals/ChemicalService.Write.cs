using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Chemicals;

public sealed partial class ChemicalService
{
    // ---------- Создание / редактирование ----------

    public async Task<ChemicalDetailDto> CreateAsync(CreateChemicalRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.Name), "Название химии обязательно.");

        var cropIds = await ValidateCropsAsync(request.CropIds, ct);

        var now = _clock.GetUtcNow();
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            CompanyId = Domain.Constants.SystemIds.DefaultCompanyId, // ТЗ §12, §25
            ItemType = ItemType.Chemical,
            Status = ItemStatus.Active,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
            ChemicalDetails = new ChemicalDetails
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                Manufacturer = Trim(request.Manufacturer),
                Comment = Trim(request.Comment),
            },
            ChemicalCrops = cropIds.Select(cid => new ChemicalCrop { CropId = cid }).ToList(),
        };

        _db.InventoryItems.Add(item);
        _audit.Log(AuditAction.Create, EntityType, item.Id,
            null, new { item.Name, item.ChemicalDetails.Type, item.ChemicalDetails.Manufacturer, CropIds = cropIds });
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(item.Id, ct);
    }

    public async Task<ChemicalDetailDto> UpdateAsync(Guid id, UpdateChemicalRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ValidationException(nameof(request.Name), "Название химии обязательно.");

        var cropIds = await ValidateCropsAsync(request.CropIds, ct);

        var item = await _db.InventoryItems
            .Include(i => i.ChemicalDetails)
            .Include(i => i.ChemicalCrops)
            .FirstOrDefaultAsync(i => i.Id == id && i.ItemType == ItemType.Chemical, ct)
            ?? throw NotFoundException.For("Химия", id);

        if (item.Status == ItemStatus.Merged)
            throw new ConflictException("Эта карточка объединена с другой и не редактируется.");

        item.ChemicalDetails ??= new ChemicalDetails { Id = Guid.NewGuid(), InventoryItemId = item.Id };

        var old = new { item.Name, item.ChemicalDetails.Type, item.ChemicalDetails.Manufacturer, item.ChemicalDetails.Comment,
            CropIds = item.ChemicalCrops.Select(cc => cc.CropId).OrderBy(x => x).ToList() };

        item.Name = name;
        item.ChemicalDetails.Type = request.Type;
        item.ChemicalDetails.Manufacturer = Trim(request.Manufacturer);
        item.ChemicalDetails.Comment = Trim(request.Comment);
        item.UpdatedAt = _clock.GetUtcNow();
        ReconcileCrops(item, cropIds);

        _audit.Log(AuditAction.Update, EntityType, item.Id, old,
            new { item.Name, item.ChemicalDetails.Type, item.ChemicalDetails.Manufacturer, item.ChemicalDetails.Comment, CropIds = cropIds });
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(item.Id, ct);
    }

    // ---------- Архив ----------

    public async Task ArchiveAsync(Guid id, ArchiveChemicalRequest request, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.ItemType == ItemType.Chemical, ct)
            ?? throw NotFoundException.For("Химия", id);

        if (item.Status == ItemStatus.Archived)
            throw new ConflictException("Химия уже в архиве.");
        if (item.Status == ItemStatus.Merged)
            throw new ConflictException("Объединённую карточку нельзя архивировать.");

        var total = await _db.ChemicalStockBalances
            .Where(b => b.ChemicalId == id).SumAsync(b => (decimal?)b.TotalLiters, ct) ?? 0m;

        // При остатке > 0 требуется ручное подтверждение словом «АРХИВ» (ТЗ §17.1).
        if (total > 0 && !string.Equals(request.Confirmation?.Trim(), "АРХИВ", StringComparison.Ordinal))
            throw new ConflictException(
                $"У химии остаток {total:0.###} л. Для архивирования введите слово «АРХИВ».");

        item.Status = ItemStatus.Archived;
        item.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Archive, EntityType, item.Id,
            new { Status = nameof(ItemStatus.Active) }, new { Status = nameof(ItemStatus.Archived), TotalLiters = total });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.ItemType == ItemType.Chemical, ct)
            ?? throw NotFoundException.For("Химия", id);

        if (item.Status != ItemStatus.Archived)
            throw new ConflictException("Восстановить можно только карточку из архива.");

        item.Status = ItemStatus.Active;
        item.UpdatedAt = _clock.GetUtcNow();

        _audit.Log(AuditAction.Restore, EntityType, item.Id,
            new { Status = nameof(ItemStatus.Archived) }, new { Status = nameof(ItemStatus.Active) });
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Объединение дублей (ТЗ §18.2) ----------

    public async Task<ChemicalDetailDto> MergeAsync(MergeChemicalsRequest request, CancellationToken ct = default)
    {
        var sourceIds = (request.SourceIds ?? Array.Empty<Guid>()).Distinct().Where(sid => sid != request.TargetId).ToList();
        if (sourceIds.Count == 0)
            throw new ValidationException(nameof(request.SourceIds), "Не выбраны карточки для объединения.");

        var target = await _db.InventoryItems
            .Include(i => i.ChemicalDetails)
            .Include(i => i.ChemicalCrops)
            .FirstOrDefaultAsync(i => i.Id == request.TargetId && i.ItemType == ItemType.Chemical, ct)
            ?? throw NotFoundException.For("Основная химия", request.TargetId);

        if (target.Status == ItemStatus.Merged)
            throw new ConflictException("Основная карточка сама является объединённой.");

        var sources = await _db.InventoryItems
            .Where(i => sourceIds.Contains(i.Id) && i.ItemType == ItemType.Chemical)
            .ToListAsync(ct);

        if (sources.Count != sourceIds.Count)
            throw new ConflictException("Некоторые выбранные карточки не найдены.");
        if (sources.Any(s => s.Status == ItemStatus.Merged))
            throw new ConflictException("Одна из выбранных карточек уже объединена.");

        var cropIds = await ValidateCropsAsync(request.CropIds, ct);
        var now = _clock.GetUtcNow();

        // Переносим остатки и операции всех дублей на основную карточку.
        foreach (var source in sources)
            await ReassignStockAsync(source.Id, target.Id, ct);

        // Итоговые поля выбирает пользователь (ТЗ §18.3).
        target.ChemicalDetails ??= new ChemicalDetails { Id = Guid.NewGuid(), InventoryItemId = target.Id };
        target.ChemicalDetails.Type = request.Type;
        target.ChemicalDetails.Manufacturer = Trim(request.Manufacturer);
        target.ChemicalDetails.Comment = Trim(request.Comment);
        target.UpdatedAt = now;
        ReconcileCrops(target, cropIds);

        foreach (var source in sources)
        {
            source.Status = ItemStatus.Merged;
            source.MergedIntoItemId = target.Id;
            source.UpdatedAt = now;
        }

        _audit.Log(AuditAction.Merge, EntityType, target.Id,
            new { SourceIds = sourceIds },
            new { TargetId = target.Id, target.Name, CropIds = cropIds });
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(target.Id, ct);
    }

    // ---------- Приватные помощники ----------

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<List<Guid>> ValidateCropsAsync(IReadOnlyList<Guid>? cropIds, CancellationToken ct)
    {
        var ids = (cropIds ?? Array.Empty<Guid>()).Distinct().ToList();
        // Культура обязательна для карточки химии (ТЗ §7.3).
        if (ids.Count == 0)
            throw new ValidationException(nameof(CreateChemicalRequest.CropIds), "Укажите хотя бы одну культуру.");

        var existing = await _db.Crops.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        var missing = ids.Except(existing).ToList();
        if (missing.Count > 0)
            throw new ValidationException(nameof(CreateChemicalRequest.CropIds), "Некоторые культуры не найдены.");

        return ids;
    }

    private void ReconcileCrops(InventoryItem item, List<Guid> cropIds)
    {
        var current = item.ChemicalCrops.ToList();
        foreach (var link in current.Where(cc => !cropIds.Contains(cc.CropId)))
            item.ChemicalCrops.Remove(link);

        var existingIds = item.ChemicalCrops.Select(cc => cc.CropId).ToHashSet();
        foreach (var cid in cropIds.Where(cid => !existingIds.Contains(cid)))
            item.ChemicalCrops.Add(new ChemicalCrop { ChemicalId = item.Id, CropId = cid });
    }

    /// <summary>Переносит остатки/упаковки/операции с дубля на основную карточку.</summary>
    private async Task ReassignStockAsync(Guid sourceId, Guid targetId, CancellationToken ct)
    {
        var groups = await _db.PackageGroups.Where(g => g.ChemicalId == sourceId).ToListAsync(ct);
        foreach (var g in groups) g.ChemicalId = targetId;

        var opened = await _db.OpenedPackages.Where(o => o.ChemicalId == sourceId).ToListAsync(ct);
        foreach (var o in opened) o.ChemicalId = targetId;

        var movements = await _db.InventoryMovements.Where(m => m.ChemicalId == sourceId).ToListAsync(ct);
        foreach (var m in movements) m.ChemicalId = targetId;

        // Балансы уникальны по паре химия+склад: складываем при совпадении склада.
        var sourceBalances = await _db.ChemicalStockBalances.Where(b => b.ChemicalId == sourceId).ToListAsync(ct);
        var targetBalances = await _db.ChemicalStockBalances.Where(b => b.ChemicalId == targetId).ToListAsync(ct);
        foreach (var sb in sourceBalances)
        {
            var tb = targetBalances.FirstOrDefault(b => b.WarehouseId == sb.WarehouseId);
            if (tb is null)
            {
                sb.ChemicalId = targetId;
                targetBalances.Add(sb);
            }
            else
            {
                tb.LooseLiters += sb.LooseLiters;
                tb.TotalLiters += sb.TotalLiters;
                tb.UpdatedAt = _clock.GetUtcNow();
                _db.ChemicalStockBalances.Remove(sb);
            }
        }
    }
}
