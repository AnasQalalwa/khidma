namespace Khidma.Api.Services.Documents;

public sealed class ProviderDocumentStorageOptions
{
    public string? RootPath { get; set; }
}

public interface IProviderDocumentStorage
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
