using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly TMPMSDbContext _context;
        public NewsArticleRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<NewsArticle>> GetAllAsync(string? tag)
        {
            var query = _context.NewsArticles.Where(a => a.IsActive);
            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(a => a.Tag == tag);
            }
            return await query.OrderByDescending(a => a.PublishedDate).ToListAsync();
        }

        public async Task<NewsArticle?> GetByIdAsync(int id) => await _context.NewsArticles.FindAsync(id);

        public async Task<NewsArticle> CreateAsync(NewsArticle article)
        {
            _context.NewsArticles.Add(article);
            await _context.SaveChangesAsync();
            return article;
        }

        public async Task<NewsArticle?> UpdateAsync(NewsArticle article)
        {
            _context.NewsArticles.Update(article);
            await _context.SaveChangesAsync();
            return article;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null) return false;
            _context.NewsArticles.Remove(article);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
