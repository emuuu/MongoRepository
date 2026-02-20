namespace MongoRepository.Docs.Models;

public class SearchEntry
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string PlainText { get; set; } = "";
    public List<string> Headings { get; set; } = [];
}
