namespace DocumentArchive.Infrastructure.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string DataPath { get; set; } = "App_Data"; // относительный путь
}