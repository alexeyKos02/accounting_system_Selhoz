using AgroInventory.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>
/// Health-эндпоинты (ТЗ §29). Используются фронтом, в т.ч. для аварийного экрана
/// восстановления: если БД недоступна/без схемы или бэкапы доступны из S3 — фронт реагирует.
/// </summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthService _databaseHealth;
    private readonly IBackupStorage _backupStorage;

    public HealthController(IDatabaseHealthService databaseHealth, IBackupStorage backupStorage)
    {
        _databaseHealth = databaseHealth;
        _backupStorage = backupStorage;
    }

    /// <summary>Общий статус сервиса.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "AgroInventory.Api", utc = DateTimeOffset.UtcNow });

    /// <summary>Статус подключения к БД и готовности схемы.</summary>
    [HttpGet("database")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        var health = await _databaseHealth.CheckAsync(ct);
        var payload = new
        {
            canConnect = health.CanConnect,
            schemaReady = health.SchemaReady,
            error = health.Error
        };

        // Доступность БД для аварийного экрана: не 503 при отсутствии схемы,
        // чтобы фронт мог отличить "БД жива, но пустая" от "БД недоступна".
        return health.CanConnect ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }

    /// <summary>Список последних бэкапов из S3 (источник истины — хранилище, а не БД).</summary>
    [HttpGet("backups")]
    public async Task<IActionResult> Backups(CancellationToken ct)
    {
        var backups = await _backupStorage.GetBackupsAsync(ct);
        return Ok(new { count = backups.Count, backups });
    }
}
