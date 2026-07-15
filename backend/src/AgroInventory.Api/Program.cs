using System.Text;
using AgroInventory.Api.Security;
using AgroInventory.Application;
using AgroInventory.Infrastructure;
using AgroInventory.Infrastructure.Persistence;
using AgroInventory.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Локальные переопределения с секретами (ключи S3, GPT и т.п.). Файл не коммитится
// (.gitignore: appsettings.*.local.json), поэтому подходит для приватной конфигурации on-prem.
// В облаке те же ключи можно задавать переменными окружения (Backup__S3__AccessKey и т.д.).
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true, reloadOnChange: true);

const string CorsPolicy = "AgroInventoryFrontend";

builder.Services.AddControllers(options =>
    options.Filters.AddService<AgroInventory.Api.Security.CompanyAccessFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroInventory API",
        Version = "v1",
        Description = "Учёт складских остатков агрохимии."
    });

    // Bearer-авторизация в Swagger UI: кнопка Authorize для JWT (ТЗ §1).
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    o.AddSecurityDefinition("Bearer", scheme);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
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

// Аутентификация JWT Bearer (ТЗ §1). Параметры — из секции Jwt (ключ подписи в проде из env).
var authOptions = new AuthOptions();
builder.Configuration.GetSection(AuthOptions.SectionName).Bind(authOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // не переименовывать "sub"/"email" в длинные URI-claim'ы
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(AuthorizationPolicies.SystemAdmin, p =>
        p.RequireClaim(JwtClaimNames.IsSystemAdmin, "true"));

    // По умолчанию все endpoints требуют аутентификации (ТЗ §24). Открытые точки
    // (вход, обновление токена, health) помечены [AllowAnonymous].
    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Action-filter проверки доступа к хозяйству и прав по атрибуту [RequireCompany] (ТЗ §5, §24).
builder.Services.AddScoped<AgroInventory.Api.Security.CompanyAccessFilter>();

var app = builder.Build();

// Применяем миграции при старте (удобно для Railway). Ошибку не роняем — приложение должно
// подняться даже при недоступной БД, чтобы отдавать health и экран восстановления (ТЗ §24.6).
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AgroInventoryDbContext>().Database.Migrate();

        // Первичный системный администратор (ТЗ §25.2). Идемпотентно.
        scope.ServiceProvider.GetRequiredService<AuthBootstrapper>()
            .EnsureSeedAdminAsync().GetAwaiter().GetResult();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
