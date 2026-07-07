using AgroInventory.Application;
using AgroInventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
