using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface INewsArticleService
    {
        Task<List<NewsArticleDto>> GetAllAsync(string? tag);
        Task<NewsArticleDto?> GetByIdAsync(int id);
        Task<NewsArticleDto> CreateAsync(NewsArticleCreateDto dto);
        Task<NewsArticleDto?> UpdateAsync(int id, NewsArticleCreateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
