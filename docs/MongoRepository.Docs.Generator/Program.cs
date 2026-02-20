using MongoRepository.Docs.Generator;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: MongoRepository.Docs.Generator <wwwroot-path> [--base-url <url>]");
    return 1;
}

var wwwrootPath = args[0];
if (!Directory.Exists(wwwrootPath))
{
    Console.Error.WriteLine($"wwwroot path not found: {wwwrootPath}");
    return 1;
}

var baseUrl = "https://emuuu.github.io/MongoRepository";
var baseUrlIndex = Array.IndexOf(args, "--base-url");
if (baseUrlIndex >= 0 && baseUrlIndex + 1 < args.Length)
{
    baseUrl = args[baseUrlIndex + 1];
}

var dataDir = Path.Combine(wwwrootPath, "data");
Directory.CreateDirectory(dataDir);

Console.WriteLine("Generating API documentation...");
var apiGenerator = new ApiDocGenerator();
await apiGenerator.GenerateAsync(Path.Combine(dataDir, "api-docs.json"));

Console.WriteLine("Generating content index...");
var contentGenerator = new ContentIndexGenerator();
var entries = await contentGenerator.GenerateAsync(
    Path.Combine(wwwrootPath, "content"),
    Path.Combine(dataDir, "content-index.json"));

Console.WriteLine("Generating static HTML pages...");
var staticHtmlGenerator = new StaticHtmlGenerator(baseUrl);
await staticHtmlGenerator.GenerateAsync(wwwrootPath, entries);

Console.WriteLine("Generating sitemap and robots.txt...");
var sitemapGenerator = new SitemapGenerator(baseUrl);
await sitemapGenerator.GenerateAsync(wwwrootPath, entries);

Console.WriteLine("Documentation data generated successfully.");
return 0;
