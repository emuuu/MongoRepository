namespace MongoRepository.Docs.Models;

public class NavSection
{
    public string Category { get; set; } = "";
    public List<NavItem> Items { get; set; } = [];
}

public class NavItem
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Order { get; set; }
}
