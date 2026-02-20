using MongoRepository.Docs.Models;

namespace MongoRepository.Docs.Services;

public interface IApiDocService
{
    Task InitializeAsync();
    ApiTypeDoc? GetApiDoc(string typeName);
}
