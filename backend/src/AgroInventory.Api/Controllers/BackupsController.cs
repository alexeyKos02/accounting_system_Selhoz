using AgroInventory.Api.Security;
using AgroInventory.Application.Abstractions;
using AgroInventory.Application.Backups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroInventory.Api.Controllers;

/// <summary>Резервные копии БД: список, создание, скачивание, восстановление (ТЗ §24). Только SystemAdmin.</summary>
[ApiController]
[Route("api/backups")]
[Authorize(Policy = AuthorizationPolicies.SystemAdmin)]
public sealed class BackupsController : ControllerBase
{
    private readonly IBackupService _service;

    public BackupsController(IBackupService service) => _service = service;

    /// <summary>Список бэкапов + признак «хранилище настроено».</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var backups = await _service.ListAsync(ct);
        return Ok(new { configured = _service.IsConfigured, backups });
    }

    [HttpPost]
    public async Task<BackupInfo> Create(CancellationToken ct) => await _service.CreateAsync(ct);

    [HttpGet("{fileName}/download")]
    public async Task<IActionResult> Download(string fileName, CancellationToken ct)
    {
        var stream = await _service.DownloadAsync(fileName, ct);
        return File(stream, "application/json", fileName);
    }

    /// <summary>Восстановление БД из бэкапа — опасное действие (ТЗ §24.6).</summary>
    [HttpPost("{fileName}/restore")]
    public async Task<BackupRestoreResultDto> Restore(string fileName, CancellationToken ct) =>
        await _service.RestoreAsync(fileName, ct);
}
