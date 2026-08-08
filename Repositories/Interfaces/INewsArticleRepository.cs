using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface INewsArticleRepository
    {
        Task<List<NewsArticle>> GetAllAsync(string? tag);
        Task<NewsArticle?> GetByIdAsync(int id);
        Task<NewsArticle> CreateAsync(NewsArticle article);
        Task<NewsArticle?> UpdateAsync(NewsArticle article);
        Task<bool> DeleteAsync(int id);
    }
}
