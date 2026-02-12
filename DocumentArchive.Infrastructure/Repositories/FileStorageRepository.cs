using System.Text.Json;
using System.Collections.Concurrent;

namespace DocumentArchive.Infrastructure.Repositories;

public abstract class FileStorageRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private ConcurrentDictionary<Guid, T> _items = new();
    private readonly Func<T, Guid> _getId;

    // Добавляем необязательный параметр dataDirectory
    protected FileStorageRepository(string fileName, Func<T, Guid> getId, string? dataDirectory = null)
    {
        var baseDirectory = dataDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        if (!Directory.Exists(baseDirectory))
            Directory.CreateDirectory(baseDirectory);
        
        _filePath = Path.Combine(baseDirectory, fileName);
        _getId = getId;
        LoadFromFile().Wait();
    }
    private async Task LoadFromFile()
    {
        if (!File.Exists(_filePath))
        {
            _items = new ConcurrentDictionary<Guid, T>();
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        var list = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        _items = new ConcurrentDictionary<Guid, T>(list.ToDictionary(_getId, x => x));
    }

    private async Task SaveToFileAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var list = _items.Values.ToList();
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });

            // атомарная запись через временный файл
            var tempFile = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _filePath, true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync() => await Task.FromResult(_items.Values);

    public async Task<T?> GetByIdAsync(Guid id) =>
        _items.TryGetValue(id, out var item) ? item : null;

    public async Task AddAsync(T entity)
    {
        var id = _getId(entity);
        if (!_items.TryAdd(id, entity))
            throw new InvalidOperationException($"Entity with id {id} already exists.");
        await SaveToFileAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        var id = _getId(entity);
        _items[id] = entity;
        await SaveToFileAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        if (_items.TryRemove(id, out _))
            await SaveToFileAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate) =>
        await Task.FromResult(_items.Values.Where(predicate));
}