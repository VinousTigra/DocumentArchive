using DocumentArchive.Core.Interfaces;
using DocumentArchive.Infrastructure.Configuration;
using DocumentArchive.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocumentArchive.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));

        services.AddScoped<IDocumentRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>();
            return new DocumentRepository(options);
        });

        services.AddScoped<IUserRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>();
            return new UserRepository(options);
        });

        services.AddScoped<ICategoryRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>();
            return new CategoryRepository(options);
        });

        services.AddScoped<IArchiveLogRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>();
            return new ArchiveLogRepository(options);
        });

        return services;
    }
}