using AutoMapper;
using DocumentArchive.Services.Mapping;

namespace DocumentArchive.Tests.ServicesTests;

public static class TestHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }
}