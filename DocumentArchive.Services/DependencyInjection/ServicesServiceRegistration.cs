using DocumentArchive.Services.Mapping;
using DocumentArchive.Services.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentArchive.Services.DependencyInjection;

public static class ServicesServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // FluentValidation: регистрируем все валидаторы из сборки, где есть любой валидатор
        services.AddValidatorsFromAssemblyContaining<CreateDocumentDtoValidator>();

        return services;
    }
}