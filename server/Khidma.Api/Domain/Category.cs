namespace Khidma.Api.Domain;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public ICollection<Service> Services { get; set; }
        = new List<Service>();
}
