using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class NewsArticleService : INewsArticleService
    {
        private readonly INewsArticleRepository _repo;
        public NewsArticleService(INewsArticleRepository repo) => _repo = repo;

        private static NewsArticleDto ToDto(NewsArticle a) => new NewsArticleDto
        {
            Id = a.Id,
            Title = a.Title,
            Excerpt = a.Excerpt,
            Content = a.Content,
            Tag = a.Tag,
            ImageUrl = a.ImageUrl,
            PublishedDate = a.PublishedDate,
            IsActive = a.IsActive
        };

        public async Task<List<NewsArticleDto>> GetAllAsync(string? tag)
        {
            var list = await _repo.GetAllAsync(tag);
            return list.Select(ToDto).ToList();
        }

        public async Task<NewsArticleDto?> GetByIdAsync(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            return a == null ? null : ToDto(a);
        }

        public async Task<NewsArticleDto> CreateAsync(NewsArticleCreateDto dto)
        {
            var entity = new NewsArticle
            {
                Title = dto.Title,
                Excerpt = dto.Excerpt,
                Content = dto.Content,
                Tag = dto.Tag,
                ImageUrl = dto.ImageUrl,
                PublishedDate = dto.PublishedDate ?? DateTime.UtcNow,
                IsActive = dto.IsActive
            };
            var created = await _repo.CreateAsync(entity);
            return ToDto(created);
        }

        public async Task<NewsArticleDto?> UpdateAsync(int id, NewsArticleCreateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Title = dto.Title;
            existing.Excerpt = dto.Excerpt;
            existing.Content = dto.Content;
            existing.Tag = dto.Tag;
            existing.ImageUrl = dto.ImageUrl;
            existing.PublishedDate = dto.PublishedDate ?? existing.PublishedDate;
            existing.IsActive = dto.IsActive;

            var updated = await _repo.UpdateAsync(existing);
            return updated == null ? null : ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);
    }
}
