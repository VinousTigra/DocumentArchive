namespace DocumentArchive.Core.DTOs.Shared;

public class BulkOperationResult<TKey> where TKey : notnull
{
    public List<BulkOperationItem<TKey>> Results { get; set; } = new();
    public int TotalCount => Results.Count;
    public int SuccessCount => Results.Count(r => r.Success);
    public int ErrorCount => Results.Count(r => !r.Success);
}

public class BulkOperationItem<TKey>
{
    public TKey? Id { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}