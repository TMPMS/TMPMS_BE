using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using Moq;
using Repositories.Interfaces;
using TMPMS.DTOs;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    // Test thuật toán chấm điểm Biện chứng luận trị (ClassifyAsync) — logic cốt lõi của tính năng
    // Tự chẩn đoán Đông Y. Repository giả lập bằng Moq, không cần DB thật.
    public class DiagnosisServiceClassifyTests
    {
        private static Mock<IDiagnosisRepository> CreateRepoMock(
            List<SyndromeType> syndromes, List<AnswerScoreMapping> mappings)
        {
            var repo = new Mock<IDiagnosisRepository>();
            repo.Setup(r => r.GetSyndromeTypesAsync()).ReturnsAsync(syndromes);
            repo.Setup(r => r.GetScoreMappingsAsync()).ReturnsAsync(mappings);
            return repo;
        }

        [Fact]
        public async Task ClassifyAsync_AllScoresBelowThreshold_ReturnsUnclear()
        {
            var syndromes = new List<SyndromeType>
            {
                new() { Id = 1, Code = "CAN_KHI_UAT_KET", Name = "Can khí uất kết" },
            };
            var mappings = new List<AnswerScoreMapping>
            {
                new() { AnswerOptionId = 1, SyndromeTypeId = 1, Points = 2 }, // < ngưỡng 5
            };
            var repo = CreateRepoMock(syndromes, mappings);
            var sut = new DiagnosisService(repo.Object);

            var request = new DiagnosisClassifyRequestDTO
            {
                Answers = new List<AnswerSubmissionDTO> { new() { QuestionId = 1, AnswerOptionId = 1 } }
            };

            var result = await sut.ClassifyAsync(request, currentUserId: null);

            Assert.Equal("UNCLEAR", result.PrimarySyndrome.Code);
        }

        [Fact]
        public async Task ClassifyAsync_ClearWinner_ReturnsSinglePrimarySyndrome()
        {
            var syndromes = new List<SyndromeType>
            {
                new() { Id = 1, Code = "TY_KHI_HU", Name = "Tỳ khí hư", Description = "d1", RecommendationText = "r1" },
                new() { Id = 2, Code = "THAN_DUONG_HU", Name = "Thận dương hư", Description = "d2", RecommendationText = "r2" },
            };
            var mappings = new List<AnswerScoreMapping>
            {
                new() { AnswerOptionId = 1, SyndromeTypeId = 1, Points = 10 },
                new() { AnswerOptionId = 2, SyndromeTypeId = 2, Points = 2 }, // cách xa >20% điểm cao nhất
            };
            var repo = CreateRepoMock(syndromes, mappings);
            var sut = new DiagnosisService(repo.Object);

            var request = new DiagnosisClassifyRequestDTO
            {
                Answers = new List<AnswerSubmissionDTO>
                {
                    new() { QuestionId = 1, AnswerOptionId = 1 },
                    new() { QuestionId = 2, AnswerOptionId = 2 },
                }
            };

            var result = await sut.ClassifyAsync(request, currentUserId: null);

            Assert.Equal("TY_KHI_HU", result.PrimarySyndrome.Code);
            Assert.Null(result.SecondarySyndrome);
        }

        [Fact]
        public async Task ClassifyAsync_CloseScores_MergesSecondarySyndrome()
        {
            var syndromes = new List<SyndromeType>
            {
                new() { Id = 1, Code = "A", Name = "Thể A", Description = "dA", RecommendationText = "rA" },
                new() { Id = 2, Code = "B", Name = "Thể B", Description = "dB", RecommendationText = "rB" },
            };
            // Điểm A=10, B=9 -> chênh lệch 1 < 20% của 10 (=2) -> phải gộp thể phụ B.
            var mappings = new List<AnswerScoreMapping>
            {
                new() { AnswerOptionId = 1, SyndromeTypeId = 1, Points = 10 },
                new() { AnswerOptionId = 2, SyndromeTypeId = 2, Points = 9 },
            };
            var repo = CreateRepoMock(syndromes, mappings);
            var sut = new DiagnosisService(repo.Object);

            var request = new DiagnosisClassifyRequestDTO
            {
                Answers = new List<AnswerSubmissionDTO>
                {
                    new() { QuestionId = 1, AnswerOptionId = 1 },
                    new() { QuestionId = 2, AnswerOptionId = 2 },
                }
            };

            var result = await sut.ClassifyAsync(request, currentUserId: null);

            Assert.Equal("A", result.PrimarySyndrome.Code);
            Assert.NotNull(result.SecondarySyndrome);
            Assert.Equal("B", result.SecondarySyndrome!.Code);
        }

        [Fact]
        public async Task ClassifyAsync_AnonymousUser_DoesNotPersistDiagnosis()
        {
            var syndromes = new List<SyndromeType> { new() { Id = 1, Code = "A", Name = "A", Description = "d", RecommendationText = "r" } };
            var mappings = new List<AnswerScoreMapping> { new() { AnswerOptionId = 1, SyndromeTypeId = 1, Points = 10 } };
            var repo = CreateRepoMock(syndromes, mappings);
            var sut = new DiagnosisService(repo.Object);

            var request = new DiagnosisClassifyRequestDTO
            {
                Answers = new List<AnswerSubmissionDTO> { new() { QuestionId = 1, AnswerOptionId = 1 } }
            };

            await sut.ClassifyAsync(request, currentUserId: null);

            repo.Verify(r => r.Create(It.IsAny<Diagnosis>()), Times.Never);
        }

        [Fact]
        public async Task ClassifyAsync_LoggedInUser_PersistsDiagnosisSnapshot()
        {
            var syndromes = new List<SyndromeType> { new() { Id = 1, Code = "A", Name = "A", Description = "d", RecommendationText = "r" } };
            var mappings = new List<AnswerScoreMapping> { new() { AnswerOptionId = 1, SyndromeTypeId = 1, Points = 10 } };
            var repo = CreateRepoMock(syndromes, mappings);
            repo.Setup(r => r.Create(It.IsAny<Diagnosis>())).ReturnsAsync((Diagnosis d) => d);
            var sut = new DiagnosisService(repo.Object);

            var request = new DiagnosisClassifyRequestDTO
            {
                Answers = new List<AnswerSubmissionDTO> { new() { QuestionId = 1, AnswerOptionId = 1 } }
            };

            await sut.ClassifyAsync(request, currentUserId: 7);

            repo.Verify(r => r.Create(It.Is<Diagnosis>(d => d.PatientId == 7)), Times.Once);
        }
    }
}
