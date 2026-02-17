using System.Reflection;
using DocumentArchive.API.Middleware;
using DocumentArchive.Infrastructure.DependencyInjection;
using DocumentArchive.Services.DependencyInjection;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры с FluentValidation и настройками JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true; // красивое форматирование JSON
    });

// регистрация FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Передаём конфигурацию в метод расширения Infrastructure 
builder.Services.AddInfrastructure(builder.Configuration);

// Регистрация слоя Services (AutoMapper, валидаторы, сервисы)
builder.Services.AddServices();

// Настройка Swagger (Swashbuckle)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Electronic Archive API",
        Version = "v1",
        Description = "ASP.NET Core Web API для управления электронным архивом документов",
        Contact = new OpenApiContact
        {
            Name = "Student Name",
            Email = "student@example.com"
        }
    });

    // Включение XML-комментариев для документации
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Настройка аутентификации через JWT (для будущих лабораторных)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Конфигурация конвейера обработки HTTP-запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Electronic Archive API v1");
    });
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