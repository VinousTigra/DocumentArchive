using DocumentArchive.Infrastructure.DependencyInjection;
using DocumentArchive.Services.DependencyInjection;
using FluentValidation.AspNetCore;
using Scalar.AspNetCore;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddFluentValidation()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Передаём Configuration в метод расширения
builder.Services.AddInfrastructure((IConfiguration)builder.Configuration);
builder.Services.AddServices();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    // Добавляем лог со ссылкой
    app.Logger.LogInformation("Scalar UI доступен по адресу http://localhost:5041/scalar/v1");
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();