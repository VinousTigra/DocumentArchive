using System.Text;
using DocumentArchive.API.Middleware;
using DocumentArchive.Core.Authorization;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Services.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// JWT настройки
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        if (secretKey != null)
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization(options =>
{
    // 1. Простая ролевая политика
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

    // 2. Политика на подтверждение email (через claim)
    options.AddPolicy("EmailConfirmed", policy => policy.RequireClaim("IsEmailConfirmed", "True"));

    // 3. Политика на возраст (кастомная)
    options.AddPolicy("MinimumAge18", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));

    // 4. Политика на право редактирования документа
    options.AddPolicy("CanEditDocument", policy =>
        policy.Requirements.Add(new PermissionRequirement("EditOwnDocuments", "EditAnyDocument")));

    // 5. Политика на право удаления документа
    options.AddPolicy("CanDeleteDocument", policy =>
        policy.Requirements.Add(new PermissionRequirement("DeleteOwnDocuments", "DeleteAnyDocument")));
});
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();


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
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();