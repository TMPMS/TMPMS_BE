using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Models;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class HealthQuizService : IHealthQuizService
    {
        private readonly TMPMSDbContext _context;

        public HealthQuizService(TMPMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<HealthQuizListDTO>> GetActiveQuizzesAsync()
        {
            return await _context.HealthQuizzes
                .Where(q => q.IsActive)
                .Select(q => new HealthQuizListDTO
                {
                    Code = q.Code,
                    Title = q.Title,
                    Description = q.Description,
                    IconUrl = q.IconUrl
                })
                .ToListAsync();
        }

        public async Task<HealthQuizDetailDTO?> GetQuizByCodeAsync(string code)
        {
            var quiz = await _context.HealthQuizzes
                .Include(q => q.Questions.OrderBy(question => question.QuestionOrder))
                    .ThenInclude(question => question.AnswerOptions.OrderBy(option => option.OptionOrder))
                .FirstOrDefaultAsync(q => q.Code.ToLower() == code.ToLower() && q.IsActive);

            if (quiz == null) return null;

            return new HealthQuizDetailDTO
            {
                Code = quiz.Code,
                Title = quiz.Title,
                Description = quiz.Description,
                IconUrl = quiz.IconUrl,
                Questions = quiz.Questions.Select(question => new QuizQuestionDTO
                {
                    Id = question.Id,
                    QuestionOrder = question.QuestionOrder,
                    QuestionText = question.QuestionText,
                    AnswerOptions = question.AnswerOptions.Select(option => new QuizAnswerOptionDTO
                    {
                        Id = option.Id,
                        OptionOrder = option.OptionOrder,
                        OptionText = option.OptionText,
                        Points = option.Points
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<QuizSubmitResponseDTO> SubmitQuizAsync(string code, QuizSubmitRequestDTO dto, int? userId)
        {
            var quiz = await _context.HealthQuizzes
                .Include(q => q.Questions)
                    .ThenInclude(question => question.AnswerOptions)
                .Include(q => q.ResultBands)
                .FirstOrDefaultAsync(q => q.Code.ToLower() == code.ToLower() && q.IsActive);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bài test với mã '{code}'.");
            }

            int totalScore = 0;
            var selectedAnswerIds = dto?.Answers?.Select(a => a.AnswerOptionId).ToHashSet() ?? new HashSet<int>();

            // Calculate score by summing points of selected answer options
            foreach (var question in quiz.Questions)
            {
                var chosenOption = question.AnswerOptions.FirstOrDefault(o => selectedAnswerIds.Contains(o.Id));
                if (chosenOption != null)
                {
                    totalScore += chosenOption.Points;
                }
            }

            // Find matching ResultBand (MinScore <= TotalScore <= MaxScore)
            var matchingBand = quiz.ResultBands
                .Where(b => totalScore >= b.MinScore && totalScore <= b.MaxScore)
                .OrderBy(b => b.MinScore)
                .FirstOrDefault();

            // Fallback if score exceeds highest band or falls below lowest band
            if (matchingBand == null && quiz.ResultBands.Any())
            {
                if (totalScore > quiz.ResultBands.Max(b => b.MaxScore))
                {
                    matchingBand = quiz.ResultBands.OrderByDescending(b => b.MaxScore).First();
                }
                else
                {
                    matchingBand = quiz.ResultBands.OrderBy(b => b.MinScore).First();
                }
            }

            // If user is authenticated, persist QuizSession to DB
            if (userId.HasValue && userId.Value > 0)
            {
                var session = new QuizSession
                {
                    QuizId = quiz.Id,
                    UserId = userId.Value,
                    TotalScore = totalScore,
                    ResultBandId = matchingBand?.Id,
                    ScoreSnapshotJson = JsonSerializer.Serialize(dto?.Answers),
                    CreatedAt = DateTime.UtcNow
                };

                _context.QuizSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            return new QuizSubmitResponseDTO
            {
                TotalScore = totalScore,
                ResultBand = new QuizResultBandDTO
                {
                    Label = matchingBand?.Label ?? "Kết quả tổng quát",
                    RiskLevel = matchingBand?.RiskLevel ?? "Low",
                    Description = matchingBand?.Description ?? string.Empty,
                    RecommendationText = matchingBand?.RecommendationText ?? string.Empty
                }
            };
        }
    }
}
