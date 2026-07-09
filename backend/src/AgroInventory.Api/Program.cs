using AgroInventory.Application;
using AgroInventory.Infrastructure;
using AgroInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Локальные переопределения с секретами (ключи S3, GPT и т.п.). Файл не коммитится
// (.gitignore: appsettings.*.local.json), поэтому подходит для приватной конфигурации on-prem.
// В облаке те же ключи можно задавать переменными окружения (Backup__S3__AccessKey и т.д.).
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true, reloadOnChange: true);

const string CorsPolicy = "AgroInventoryFrontend";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AgroInventory API",
        Version = "v1",
        Description = "Учёт складских остатков агрохимии (MVP)."
    });
});

// CORS для фронтенда (GitHub Pages + локальная разработка).
// Разрешённые origin'ы берутся из конфигурации Cors:AllowedOrigins.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Применяем миграции при старте (удобно для Railway). Ошибку не роняем — приложение должно
// подняться даже при недоступной БД, чтобы отдавать health и экран восстановления (ТЗ §24.6).
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AgroInventoryDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Не удалось применить миграции при старте");
    }
}

// Swagger доступен всегда: фронт генерит типы из OpenAPI (ТЗ §3).
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroInventory API v1");
    o.DocumentTitle = "AgroInventory API";
});

// Обработка доменных исключений → корректные HTTP-ответы (ProblemDetails).
app.UseMiddleware<AgroInventory.Api.Middleware.ExceptionHandlingMiddleware>();

app.UseCors(CorsPolicy);

// Заготовка под будущую авторизацию (ТЗ §6, §31.9): пока пропускаем всё.
// AddAuthentication/AddAuthorization с реальными схемами добавятся при вводе авторизации.
app.UseAuthorization();

app.MapControllers();

app.Run();
