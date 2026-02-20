namespace MongoRepository.Docs.Models;

public class DocArticle
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Category { get; set; } = "";
    public int Order { get; set; }
    public string Description { get; set; } = "";
    public string HtmlContent { get; set; } = "";
    public List<HeadingInfo> Headings { get; set; } = [];
    public string? ApiRefType { get; set; }
}

public class HeadingInfo
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int Level { get; set; }
}
