namespace Khidma.Api.Services;

public sealed class ServiceResult<T>
{
    public bool Succeeded { get; private init; }

    public T? Value { get; private init; }

    public int StatusCode { get; private init; }

    public string Title { get; private init; } = "Error";

    public string? Detail { get; private init; }

    public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
        new Dictionary<string, string[]>();

    public static ServiceResult<T> Success(T value, int statusCode = StatusCodes.Status200OK) => new()
    {
        Succeeded = true,
        Value = value,
        StatusCode = statusCode
    };

    public static ServiceResult<T> Validation(
        IReadOnlyDictionary<string, string[]> errors,
        string title = "One or more validation errors occurred.") => new()
    {
        Succeeded = false,
        StatusCode = StatusCodes.Status400BadRequest,
        Title = title,
        Errors = errors
    };

    public static ServiceResult<T> Validation(string field, string message) =>
        Validation(new Dictionary<string, string[]> { [field] = [message] });

    public static ServiceResult<T> NotFound(
        string title = "Not Found",
        string? detail = null) => new()
    {
        Succeeded = false,
        StatusCode = StatusCodes.Status404NotFound,
        Title = title,
        Detail = detail
    };

    public static ServiceResult<T> Forbidden(
        string title = "Forbidden",
        string? detail = null) => new()
    {
        Succeeded = false,
        StatusCode = StatusCodes.Status403Forbidden,
        Title = title,
        Detail = detail
    };

    public static ServiceResult<T> Conflict(
        string title,
        string? detail = null) => new()
    {
        Succeeded = false,
        StatusCode = StatusCodes.Status409Conflict,
        Title = title,
        Detail = detail
    };

    public static ServiceResult<T> Unauthorized(string title = "Unauthorized") => new()
    {
        Succeeded = false,
        StatusCode = StatusCodes.Status401Unauthorized,
        Title = title
    };
}
