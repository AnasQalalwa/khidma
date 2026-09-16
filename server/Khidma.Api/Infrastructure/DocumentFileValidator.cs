namespace Khidma.Api.Infrastructure;

public sealed class DocumentValidationResult
{
    public bool Succeeded { get; private init; }

    public string? Error { get; private init; }

    public string CanonicalExtension { get; private init; } = string.Empty;

    public string CanonicalContentType { get; private init; } = string.Empty;

    public string SafeOriginalFileName { get; private init; } = string.Empty;

    public long FileSizeBytes { get; private init; }

    public static DocumentValidationResult Fail(string error) => new()
    {
        Succeeded = false,
        Error = error
    };

    public static DocumentValidationResult Ok(
        string extension,
        string contentType,
        string originalFileName,
        long size) => new()
    {
        Succeeded = true,
        CanonicalExtension = extension,
        CanonicalContentType = contentType,
        SafeOriginalFileName = originalFileName,
        FileSizeBytes = size
    };
}

public static class DocumentFileValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static DocumentValidationResult Validate(IFormFile file)
    {
        if (file is null)
        {
            return DocumentValidationResult.Fail("A document file is required.");
        }

        Span<byte> header = stackalloc byte[8];
        int read;
        using (var stream = file.OpenReadStream())
        {
            read = stream.Read(header);
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        return Validate(file.FileName, file.ContentType, file.Length, header[..read]);
    }

    public static DocumentValidationResult Validate(
        string? fileName,
        string? contentType,
        long length,
        ReadOnlySpan<byte> header)
    {
        if (length <= 0)
        {
            return DocumentValidationResult.Fail("A document file is required.");
        }

        if (length > MaxFileSizeBytes)
        {
            return DocumentValidationResult.Fail("Documents must be 10 MB or smaller.");
        }

        var originalName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(originalName))
        {
            return DocumentValidationResult.Fail("The original file name is required.");
        }

        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".jpg" and not ".jpeg" and not ".png")
        {
            return DocumentValidationResult.Fail(
                "Only PDF, JPEG, and PNG documents are accepted.");
        }

        var normalizedType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedType is not "application/pdf"
            and not "image/jpeg"
            and not "image/jpg"
            and not "image/png")
        {
            return DocumentValidationResult.Fail(
                "The file type is not allowed. Upload a PDF, JPEG, or PNG.");
        }

        if (header.Length < 4)
        {
            return DocumentValidationResult.Fail("The uploaded file is not a valid document.");
        }

        var detected = DetectType(header);
        if (detected is null)
        {
            return DocumentValidationResult.Fail(
                "The file contents do not match an allowed PDF, JPEG, or PNG document.");
        }

        var (canonicalExtension, canonicalContentType) = detected.Value;
        if (!ExtensionMatches(extension, canonicalExtension) ||
            !ContentTypeMatches(normalizedType, canonicalContentType))
        {
            return DocumentValidationResult.Fail(
                "The file name, content type, and file contents do not match.");
        }

        return DocumentValidationResult.Ok(
            canonicalExtension,
            canonicalContentType,
            originalName,
            length);
    }

    private static (string Extension, string ContentType)? DetectType(ReadOnlySpan<byte> header)
    {
        if (HasPrefix(header, PdfSignature))
        {
            return (".pdf", "application/pdf");
        }

        if (HasPrefix(header, JpegSignature))
        {
            return (".jpg", "image/jpeg");
        }

        if (HasPrefix(header, PngSignature))
        {
            return (".png", "image/png");
        }

        return null;
    }

    private static bool ExtensionMatches(string provided, string canonical) =>
        canonical switch
        {
            ".pdf" => provided == ".pdf",
            ".jpg" => provided is ".jpg" or ".jpeg",
            ".png" => provided == ".png",
            _ => false
        };

    private static bool ContentTypeMatches(string provided, string canonical) =>
        canonical switch
        {
            "application/pdf" => provided == "application/pdf",
            "image/jpeg" => provided is "image/jpeg" or "image/jpg",
            "image/png" => provided == "image/png",
            _ => false
        };

    private static bool HasPrefix(ReadOnlySpan<byte> header, byte[] signature) =>
        header.Length >= signature.Length &&
        header[..signature.Length].SequenceEqual(signature);
}
