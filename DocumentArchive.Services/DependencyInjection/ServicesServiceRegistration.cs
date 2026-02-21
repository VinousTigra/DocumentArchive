using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Services;
using DocumentArchive.Services.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentArchive.Services.DependencyInjection;

public static class ServicesServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Регистрируем AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Регистрируем сервисы
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IArchiveLogService, ArchiveLogService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDocumentVersionService, DocumentVersionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();

        // Валидаторы 
        services.AddValidatorsFromAssemblyContaining<CreateDocumentDtoValidator>();
        return services;
    }
}