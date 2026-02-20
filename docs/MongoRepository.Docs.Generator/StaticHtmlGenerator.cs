using System.Text.Json;
using System.Web;

namespace MongoRepository.Docs.Generator;

public class StaticHtmlGenerator
{
    private readonly string _baseUrl;

    public StaticHtmlGenerator(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task GenerateAsync(string wwwrootPath, List<ContentIndexEntry> entries)
    {
        var count = 0;

        foreach (var entry in entries)
        {
            var dir = Path.Combine(wwwrootPath, "docs", entry.Slug);
            Directory.CreateDirectory(dir);
            var html = BuildDocPage(entry);
            await File.WriteAllTextAsync(Path.Combine(dir, "index.html"), html);
            count++;
        }

        Console.WriteLine($"  Generated {count} static doc pages");

        await InjectMetaTags(wwwrootPath);
    }

    private string BuildDocPage(ContentIndexEntry entry)
    {
        var title = HttpUtility.HtmlEncode(entry.Title);
        var description = HttpUtility.HtmlEncode(entry.Description);
        var url = $"{_baseUrl}/docs/{entry.Slug}";
        var jsonLd = BuildJsonLd(entry);

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{title} - MongoRepository Docs</title>
                <base href="/" />
                <meta name="description" content="{description}" />
                <link rel="canonical" href="{url}" />
                <meta property="og:type" content="article" />
                <meta property="og:title" content="{title} - MongoRepository" />
                <meta property="og:description" content="{description}" />
                <meta property="og:url" content="{url}" />
                <meta property="og:site_name" content="MongoRepository Docs" />
                <meta property="og:image" content="{_baseUrl}/img/mongorepository_logo.svg" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="{title} - MongoRepository" />
                <meta name="twitter:description" content="{description}" />
                <script type="application/ld+json">{jsonLd}</script>
                <link rel="icon" type="image/svg+xml" href="img/mongorepository_logo.svg" />
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                      integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />
                <link href="lib/prismjs/prism.css" rel="stylesheet" />
                <link href="css/app.css" rel="stylesheet" />
                <link href="MongoRepository.Docs.styles.css" rel="stylesheet" />
            </head>
            <body>
                <div id="app">
                    <article style="max-width:800px;margin:2rem auto;padding:0 1rem;">
                        <h1>{title}</h1>
                        <p class="lead text-muted">{description}</p>
                        {entry.HtmlContent}
                    </article>
                </div>
                <div id="blazor-error-ui">
                    An unhandled error has occurred.
                    <a href="" class="reload">Reload</a>
                    <a class="dismiss">X</a>
                </div>
                <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"
                        integrity="sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz" crossorigin="anonymous"></script>
                <script src="lib/prismjs/prism.js"></script>
                <script src="js/docs.js"></script>
                <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """;
    }

    private string BuildJsonLd(ContentIndexEntry entry)
    {
        var url = $"{_baseUrl}/docs/{entry.Slug}";
        var slugParts = entry.Slug.Split('/');
        var breadcrumbs = new List<object>
        {
            new { @type = "ListItem", position = 1, name = "Docs", item = $"{_baseUrl}/" }
        };

        if (slugParts.Length > 1)
        {
            breadcrumbs.Add(new { @type = "ListItem", position = 2, name = entry.Category, item = $"{_baseUrl}/docs/{slugParts[0]}" });
            breadcrumbs.Add(new { @type = "ListItem", position = 3, name = entry.Title, item = url });
        }
        else
        {
            breadcrumbs.Add(new { @type = "ListItem", position = 2, name = entry.Title, item = url });
        }

        var graph = new object[]
        {
            new
            {
                @context = "https://schema.org",
                @type = "TechArticle",
                headline = entry.Title,
                description = entry.Description,
                url,
                image = $"{_baseUrl}/img/mongorepository_logo.svg",
                publisher = new { @type = "Organization", name = "MongoRepository" }
            },
            new
            {
                @context = "https://schema.org",
                @type = "BreadcrumbList",
                itemListElement = breadcrumbs
            }
        };

        return JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task InjectMetaTags(string wwwrootPath)
    {
        const string metaTags = """

                <meta name="description" content="Generic, extensible CRUD repository for MongoDB with .NET. Straightforward data access with the repository pattern." />
                <meta property="og:type" content="website" />
                <meta property="og:title" content="MongoRepository Docs" />
                <meta property="og:description" content="Generic, extensible CRUD repository for MongoDB with .NET. Straightforward data access with the repository pattern." />
                <meta property="og:site_name" content="MongoRepository Docs" />
                <meta property="og:image" content="{0}/img/mongorepository_logo.svg" />
                <meta name="twitter:card" content="summary" />
                <meta name="twitter:title" content="MongoRepository Docs" />
                <meta name="twitter:description" content="Generic, extensible CRUD repository for MongoDB with .NET." />
            """;

        var formattedTags = metaTags.Replace("{0}", _baseUrl);

        foreach (var fileName in new[] { "index.html", "404.html" })
        {
            var filePath = Path.Combine(wwwrootPath, fileName);
            if (!File.Exists(filePath)) continue;

            var content = await File.ReadAllTextAsync(filePath);

            if (content.Contains("og:title")) continue;

            const string marker = """<meta name="viewport" content="width=device-width, initial-scale=1.0" />""";
            content = content.Replace(marker, marker + formattedTags);

            await File.WriteAllTextAsync(filePath, content);
            Console.WriteLine($"  Injected meta tags into {fileName}");
        }
    }
}
