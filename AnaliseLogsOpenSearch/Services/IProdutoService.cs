using AnaliseLogsOpenSearch.Models;

namespace AnaliseLogsOpenSearch.Services;

public interface IProdutoService
{
    Task<Produto?> InsertAsync(Produto? produto);
    Task<Produto?> GetByIdAsync(string id); // String conforme o Controller espera
    Task<IReadOnlyCollection<Produto>?> FilterAsync(string text);
    Task<IReadOnlyCollection<Produto>?> GetAllAsync();
    Task<bool> UpdateAsync(string id, Produto produto); // String conforme o Controller espera
    Task<bool> DeleteAsync(string id); // String conforme o Controller espera
}
