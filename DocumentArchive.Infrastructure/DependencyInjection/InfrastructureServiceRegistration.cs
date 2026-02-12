using DocumentArchive.Core.Interfaces;
using DocumentArchive.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentArchive.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IArchiveLogRepository, ArchiveLogRepository>();

        return services;
    }
}