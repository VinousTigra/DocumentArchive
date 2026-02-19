using DocumentArchive.API.Middleware;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Настройка контроллеров и JSON-сериализации
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.WriteIndented = true; });

// Передаём конфигурацию 
builder.Services.AddServices();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("DocumentArchive.Infrastructure")));

// Настройка OpenAPI (Scalar)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) => // неиспользуемые параметры заменены на _
    {
        document.Info = new OpenApiInfo
        {
            Title = "Archive API",
            Version = "v1",
            Description = "ASP.NET Core Web API для управления архивом документов",
            Contact = new OpenApiContact
            {
                Name = "Student Name",
                Email = "student@example.com"
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Конфигурация конвейера обработки HTTP-запросов
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.Logger.LogInformation("Scalar UI доступен по адресу http://localhost:5041/scalar/v1");
}
else
{
    app.UseHsts(); // Защита в production
}

// Глобальный middleware для обработки исключений (должен быть до других middleware)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Простая настройка CORS (для разработки можно разрешить всё)
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapControllers();

app.Run();