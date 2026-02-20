using System.Net.Http.Json;
using System.Text.RegularExpressions;
using MongoRepository.Docs.Models;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MongoRepository.Docs.Services;

public partial class DocContentService : IDocContentService
{
    private readonly HttpClient _http;
    private ContentIndex[]? _index;
    private List<NavSection>? _navSections;
    private List<SearchEntry>? _searchEntries;
    private readonly Dictionary<string, DocArticle> _cache = new();
    private readonly MarkdownPipeline _pipeline;

    public DocContentService(HttpClient http)
    {
        _http = http;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers()
            .DisableHtml()
            .Build();
    }

    public async Task InitializeAsync()
    {
        if (_index is not null) return;

        try
        {
            _index = await _http.GetFromJsonAsync<ContentIndex[]>("data/content-index.json") ?? [];
        }
        catch (HttpRequestException)
        {
            _index = [];
        }

        BuildNavSections();
        BuildSearchEntries();
    }

    public List<NavSection> GetNavSections() => _navSections ?? [];

    public List<SearchEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || _searchEntries is null)
            return [];

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<(SearchEntry Entry, int Score)>();

        foreach (var entry in _searchEntries)
        {
            var score = 0;
            var titleLower = entry.Title.ToLowerInvariant();
            var textLower = entry.PlainText.ToLowerInvariant();

            foreach (var term in terms)
            {
                if (titleLower.Contains(term))
                    score += 10;
                if (entry.Headings.Any(h => h.ToLowerInvariant().Contains(term)))
                    score += 5;
                if (textLower.Contains(term))
                    score += 1;
            }

            if (score > 0)
                results.Add((entry, score));
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(20)
            .Select(r => r.Entry)
            .ToList();
    }

    public async Task<DocArticle?> GetArticleAsync(string slug)
    {
        if (_cache.TryGetValue(slug, out var cached))
            return cached;

        string md;
        try
        {
            md = await _http.GetStringAsync($"content/{slug}.md");
        }
        catch (HttpRequestException)
        {
            return null;
        }

        var (frontMatter, body) = ParseFrontMatter(md);
        var html = Markdig.Markdown.ToHtml(body, _pipeline);
        var headings = ExtractHeadings(html);

        var article = new DocArticle
        {
            Slug = slug,
            Title = frontMatter?.Title ?? slug.Split('/').Last(),
            Category = frontMatter?.Category ?? "",
            Order = frontMatter?.Order ?? 0,
            Description = frontMatter?.Description ?? "",
            HtmlContent = html,
            Headings = headings,
            ApiRefType = frontMatter?.ApiRef
        };

        _cache[slug] = article;
        return article;
    }

    private (FrontMatter? FrontMatter, string Body) ParseFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---"))
            return (null, markdown);

        var endIndex = markdown.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (null, markdown);

        var yaml = markdown[3..endIndex].Trim();
        var body = markdown[(endIndex + 3)..].TrimStart();

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var fm = deserializer.Deserialize<FrontMatter>(yaml);
            return (fm, body);
        }
        catch
        {
            return (null, body);
        }
    }

    private static List<HeadingInfo> ExtractHeadings(string html)
    {
        var headings = new List<HeadingInfo>();
        var matches = HeadingRegex().Matches(html);

        foreach (Match match in matches)
        {
            headings.Add(new HeadingInfo
            {
                Level = int.Parse(match.Groups[1].Value),
                Id = match.Groups[2].Value,
                Text = StripHtmlTags(match.Groups[3].Value)
            });
        }

        return headings;
    }

    private static string StripHtmlTags(string html) =>
        HtmlTagRegex().Replace(html, "").Trim();

    private void BuildNavSections()
    {
        if (_index is null) return;

        var categoryOrder = new[] { "Getting Started", "API Reference", "Health Checks", "Guides" };

        _navSections = _index
            .GroupBy(i => i.Category)
            .OrderBy(g => Array.IndexOf(categoryOrder, g.Key) is var idx && idx >= 0 ? idx : 999)
            .Select(g => new NavSection
            {
                Category = g.Key,
                Items = g.OrderBy(i => i.Order).ThenBy(i => i.Title)
                    .Select(i => new NavItem { Title = i.Title, Slug = i.Slug, Order = i.Order })
                    .ToList()
            })
            .ToList();
    }

    private void BuildSearchEntries()
    {
        if (_index is null) return;

        _searchEntries = _index.Select(i => new SearchEntry
        {
            Slug = i.Slug,
            Title = i.Title,
            Headings = i.Headings,
            PlainText = i.SearchText
        }).ToList();
    }

    [GeneratedRegex(@"<h([2-3])\s+id=""([^""]+)""[^>]*>(.*?)</h\1>", RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    private class FrontMatter
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public int Order { get; set; }
        public string Description { get; set; } = "";
        public string? ApiRef { get; set; }
    }
}
