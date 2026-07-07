using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Chemicals;
using AgroInventory.Application.History;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Dashboard;

/// <summary>Сводка для главного экрана (ТЗ §22): что закончилось, что на исходе, последние операции.</summary>
public sealed class DashboardService
{
    private const int RecentOperationsLimit = 10;

    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var threshold = await _db.AppSettings.Select(s => s.LowStockThresholdLiters).FirstOrDefaultAsync(ct);

        // Активная химия с суммарным остатком по всем складам и признаком наличия строки баланса.
        var chemicals = await _db.InventoryItems
            .Where(i => i.ItemType == ItemType.Chemical && i.Status == ItemStatus.Active)
            .Select(i => new
            {
                i.Id,
                i.Name,
                Total = _db.ChemicalStockBalances
                    .Where(b => b.ChemicalId == i.Id)
                    .Sum(b => (decimal?)b.TotalLiters) ?? 0m,
                HasBalance = _db.ChemicalStockBalances.Any(b => b.ChemicalId == i.Id),
            })
            .ToListAsync(ct);

        // «Закончилась» — химия, у которой был остаток на складе, а теперь ноль (ТЗ §22).
        var empty = chemicals
            .Where(c => c.HasBalance && c.Total <= 0)
            .OrderBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.Total, StockStatus.Empty))
            .ToList();

        var low = chemicals
            .Where(c => c.Total > 0 && c.Total < threshold)
            .OrderBy(c => c.Total)
            .ThenBy(c => c.Name)
            .Select(c => new DashboardStockDto(c.Id, c.Name, c.Total, StockStatus.Low))
            .ToList();

        var warehouses = await _db.Warehouses.CountAsync(ct);

        var recent = await _db.InventoryMovements.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.OccurredAt)
            .Take(RecentOperationsLimit)
            .Select(m => new HistoryItemDto(
                m.Id,
                m.OccurredAt,
                m.MovementType,
                m.ChemicalId,
                m.Chemical.Name,
                m.QuantityLiters,
                m.WarehouseId,
                m.Warehouse.Number,
                m.CropId,
                m.Crop != null ? m.Crop.Name : null,
                m.Comment))
            .ToListAsync(ct);

        return new DashboardDto(
            ActiveChemicals: chemicals.Count,
            Warehouses: warehouses,
            EmptyCount: empty.Count,
            LowCount: low.Count,
            TotalLiters: chemicals.Sum(c => c.Total),
            LowStockThresholdLiters: threshold,
            Empty: empty,
            Low: low,
            RecentOperations: recent);
    }
}
