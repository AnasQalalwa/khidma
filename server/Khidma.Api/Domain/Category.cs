namespace Khidma.Api.Domain;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = "";

    public string? ImageStoredFileName { get; set; }

    public string? ImageContentType { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageStoredFileName);

    public ICollection<Service> Services { get; set; }
        = new List<Service>();
}
