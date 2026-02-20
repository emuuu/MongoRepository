using System.Net.Http.Json;
using MongoRepository.Docs.Models;

namespace MongoRepository.Docs.Services;

public class ApiDocService : IApiDocService
{
    private readonly HttpClient _http;
    private Dictionary<string, ApiTypeDoc>? _docs;

    public ApiDocService(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        if (_docs is not null) return;

        try
        {
            var all = await _http.GetFromJsonAsync<ApiTypeDoc[]>("data/api-docs.json") ?? [];
            _docs = all.ToDictionary(d => d.TypeName);
        }
        catch (HttpRequestException)
        {
            _docs = new();
        }
    }

    public ApiTypeDoc? GetApiDoc(string typeName)
        => _docs?.GetValueOrDefault(typeName);
}
