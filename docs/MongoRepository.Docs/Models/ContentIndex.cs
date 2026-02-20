namespace MongoRepository.Docs.Models;

public class ContentIndex
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public int Order { get; set; }
    public string Description { get; set; } = "";
    public string? ApiRef { get; set; }
    public List<string> Headings { get; set; } = [];
    public string SearchText { get; set; } = "";
}
