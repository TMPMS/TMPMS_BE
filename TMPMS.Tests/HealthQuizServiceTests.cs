using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Models;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    public class HealthQuizServiceTests
    {
        private static async Task<SqliteTestDbContext> SeedQuizAsync()
        {
            var db = new SqliteTestDbContext();
            var quiz = new HealthQuiz
            {
                Code = "stress-check",
                Title = "Kiểm tra mức độ căng thẳng",
                IsActive = true,
                Questions = new List<QuizQuestion>
                {
                    new()
                    {
                        QuestionText = "Bạn có hay mất ngủ không?",
                        QuestionOrder = 1,
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new() { OptionText = "Không", OptionOrder = 1, Points = 0 },
                            new() { OptionText = "Thỉnh thoảng", OptionOrder = 2, Points = 5 },
                            new() { OptionText = "Thường xuyên", OptionOrder = 3, Points = 10 },
                        }
                    },
                    new()
                    {
                        QuestionText = "Bạn có hay đau đầu không?",
                        QuestionOrder = 2,
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new() { OptionText = "Không", OptionOrder = 1, Points = 0 },
                            new() { OptionText = "Có", OptionOrder = 2, Points = 8 },
                        }
                    }
                },
                ResultBands = new List<QuizResultBand>
                {
                    new() { MinScore = 0, MaxScore = 5, Label = "Bình thường", RiskLevel = "Low" },
                    new() { MinScore = 6, MaxScore = 15, Label = "Căng thẳng nhẹ", RiskLevel = "Medium" },
                    new() { MinScore = 16, MaxScore = 100, Label = "Căng thẳng cao", RiskLevel = "High" },
                }
            };
            db.Context.HealthQuizzes.Add(quiz);
            await db.Context.SaveChangesAsync();
            return db;
        }

        private static int OptionId(HealthQuiz quiz, int questionIndex, int optionIndex)
        {
            var questions = new List<QuizQuestion>(quiz.Questions);
            var options = new List<QuizAnswerOption>(questions[questionIndex].AnswerOptions);
            return options[optionIndex].Id;
        }

        [Fact]
        public async Task SubmitQuizAsync_SumsPointsOfSelectedOptions_AndPicksMatchingBand()
        {
            using var db = await SeedQuizAsync();
            var quiz = await db.Context.HealthQuizzes.FindAsync(1);
            var sut = new HealthQuizService(db.Context);

            var dto = new QuizSubmitRequestDTO
            {
                Answers = new List<QuizAnswerItemDTO>
                {
                    new() { QuestionId = 1, AnswerOptionId = OptionId(quiz!, 0, 1) }, // Thỉnh thoảng = 5
                    new() { QuestionId = 2, AnswerOptionId = OptionId(quiz!, 1, 0) }, // Không = 0
                }
            };

            var result = await sut.SubmitQuizAsync("stress-check", dto, userId: null);

            Assert.Equal(5, result.TotalScore);
            Assert.Equal("Bình thường", result.ResultBand.Label);
        }

        [Fact]
        public async Task SubmitQuizAsync_HighScore_PicksHighestBand()
        {
            using var db = await SeedQuizAsync();
            var quiz = await db.Context.HealthQuizzes.FindAsync(1);
            var sut = new HealthQuizService(db.Context);

            var dto = new QuizSubmitRequestDTO
            {
                Answers = new List<QuizAnswerItemDTO>
                {
                    new() { QuestionId = 1, AnswerOptionId = OptionId(quiz!, 0, 2) }, // Thường xuyên = 10
                    new() { QuestionId = 2, AnswerOptionId = OptionId(quiz!, 1, 1) }, // Có = 8
                }
            };

            var result = await sut.SubmitQuizAsync("stress-check", dto, userId: null);

            Assert.Equal(18, result.TotalScore);
            Assert.Equal("Căng thẳng cao", result.ResultBand.Label);
        }

        [Fact]
        public async Task SubmitQuizAsync_LoggedInUser_PersistsSession()
        {
            using var db = await SeedQuizAsync();
            var quiz = await db.Context.HealthQuizzes.FindAsync(1);
            var sut = new HealthQuizService(db.Context);

            var user = new BusinessObjects.User { UserName = "quizuser", Email = "quizuser@test.com" };
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var dto = new QuizSubmitRequestDTO
            {
                Answers = new List<QuizAnswerItemDTO>
                {
                    new() { QuestionId = 1, AnswerOptionId = OptionId(quiz!, 0, 0) },
                }
            };

            await sut.SubmitQuizAsync("stress-check", dto, userId: user.Id);

            var sessions = db.Context.QuizSessions;
            Assert.Equal(1, sessions.Count());
        }

        [Fact]
        public async Task SubmitQuizAsync_UnknownCode_ThrowsKeyNotFound()
        {
            using var db = await SeedQuizAsync();
            var sut = new HealthQuizService(db.Context);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.SubmitQuizAsync("does-not-exist", new QuizSubmitRequestDTO(), userId: null));
        }
    }
}
