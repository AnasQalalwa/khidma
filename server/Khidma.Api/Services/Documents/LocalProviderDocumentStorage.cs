using Microsoft.Extensions.Options;

namespace Khidma.Api.Services.Documents;

public sealed class LocalProviderDocumentStorage : IProviderDocumentStorage
{
    private readonly string _root;
    private readonly ILogger<LocalProviderDocumentStorage> _logger;

    public LocalProviderDocumentStorage(
        IOptions<ProviderDocumentStorageOptions> options,
        IHostEnvironment environment,
        ILogger<LocalProviderDocumentStorage> logger)
    {
        _logger = logger;
        _root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "provider-documents")
            : options.Value.RootPath;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);

        var stored = $"{Guid.NewGuid():N}{extension}";
        var path = ResolvePath(stored);

        await using var file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous);

        await content.CopyToAsync(file, cancellationToken);
        return stored;
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storedFileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The verification document was not found.", path);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storedFileName);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete stored document {StoredFileName}", storedFileName);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            storedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            storedFileName.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(storedFileName))
        {
            throw new InvalidOperationException("Invalid stored file name.");
        }

        var root = Path.GetFullPath(_root);
        var full = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid stored file name.");
        }

        return full;
    }
}
