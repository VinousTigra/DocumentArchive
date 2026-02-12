using DocumentArchive.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentArchive.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<UserRepository>();
        services.AddScoped<CategoryRepository>();
        services.AddScoped<DocumentRepository>();
        services.AddScoped<ArchiveLogRepository>();

        return services;
    }
}