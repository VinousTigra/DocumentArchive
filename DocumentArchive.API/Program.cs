
using DocumentArchive.API.Middleware;
using DocumentArchive.Infrastructure.DependencyInjection;
using DocumentArchive.Services.DependencyInjection;
using FluentValidation.AspNetCore; 
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Настройка контроллеров и JSON-сериализации
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
    });

// регистрация FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Передаём конфигурацию в метод расширения Infrastructure 
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddServices();


// Настройка OpenAPI (Scalar)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) => // неиспользуемые параметры заменены на _
    {
        document.Info = new()
        {
            Title = "Electronic Archive API",
            Version = "v1",
            Description = "ASP.NET Core Web API для управления электронным архивом документов",
            Contact = new()
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