using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Repositories.Interfaces;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    // FEFO (First-Expired-First-Out) là cơ chế trừ/hoàn kho quan trọng nhất hệ thống — test ở mức
    // Service với IInventoryRepository giả lập (Moq), vì repository thật dùng raw SQL
    // "WITH (UPDLOCK, ROWLOCK)" đặc thù SQL Server, không chạy được trên SQLite/InMemory.
    public class InventoryServiceFefoTests
    {
        private static Mock<IInventoryRepository> CreateRepoMock(List<StockBatch> batches)
        {
            var repo = new Mock<IInventoryRepository>();
            repo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync((IDbContextTransaction)null!);
            repo.Setup(r => r.GetActiveBatchesForFEFOForUpdate(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(() => batches.OrderBy(b => b.ExpiryDate).ToList());
            repo.Setup(r => r.AddTransaction(It.IsAny<InventoryTransaction>()))
                .ReturnsAsync((InventoryTransaction t) => t);
            repo.Setup(r => r.RecomputeStockCaches(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            // Mặc định không có giao dịch xuất gốc nào khớp (RestoreStockFEFO sẽ rơi về hành vi cũ:
            // dồn vào lô hết hạn sớm nhất) — test riêng cho việc hoàn đúng lô gốc sẽ override lại setup này.
            repo.Setup(r => r.GetExportTransactionsForReference(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<InventoryTransaction>());
            repo.Setup(r => r.GetBatchByIdForUpdate(It.IsAny<int>()))
                .ReturnsAsync((int id) => batches.FirstOrDefault(b => b.Id == id));
            return repo;
        }

        [Fact]
        public async Task DeductStockFEFO_DeductsFromEarliestExpiryBatchFirst()
        {
            var batches = new List<StockBatch>
            {
                new() { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 50, ExpiryDate = DateTime.Today.AddDays(60) },
                new() { Id = 2, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 50, ExpiryDate = DateTime.Today.AddDays(10) },
            };
            var repo = CreateRepoMock(batches);
            var sut = new InventoryService(repo.Object);

            await sut.DeductStockFEFO(10, 1, 20, "ORDER-1");

            var earlyExpiryBatch = batches.Single(b => b.Id == 2);
            var lateExpiryBatch = batches.Single(b => b.Id == 1);
            Assert.Equal(30, earlyExpiryBatch.QuantityRemaining); // trừ trước
            Assert.Equal(50, lateExpiryBatch.QuantityRemaining); // chưa động tới
        }

        [Fact]
        public async Task DeductStockFEFO_SnapshotsUnitCostPriceAtTimeOfExport_NotAffectedByLaterCostChange()
        {
            var batch = new StockBatch { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 50, ExpiryDate = DateTime.Today.AddDays(30), UnitCostPrice = 50000m };
            var batches = new List<StockBatch> { batch };
            var repo = CreateRepoMock(batches);
            var createdTransactions = new List<InventoryTransaction>();
            repo.Setup(r => r.AddTransaction(It.IsAny<InventoryTransaction>()))
                .ReturnsAsync((InventoryTransaction t) => { createdTransactions.Add(t); return t; });
            var sut = new InventoryService(repo.Object);

            await sut.DeductStockFEFO(10, 1, 20, "ORDER-9");

            var exportTx = Assert.Single(createdTransactions);
            Assert.Equal(50000m, exportTx.UnitCostPrice); // chốt đúng giá vốn lúc xuất

            // Mô phỏng nhập thêm hàng giá khác sau đó (giá vốn bình quân của lô đổi) — giao dịch xuất
            // đã ghi trước đó KHÔNG được đổi theo, vì UnitCostPrice là giá trị đã copy, không tham chiếu
            // ngược vào batch.
            batch.UnitCostPrice = 60000m;
            Assert.Equal(50000m, exportTx.UnitCostPrice);
        }

        [Fact]
        public async Task DeductStockFEFO_SpansMultipleBatches_WhenFirstBatchNotEnough()
        {
            var batches = new List<StockBatch>
            {
                new() { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 5, ExpiryDate = DateTime.Today.AddDays(10) },
                new() { Id = 2, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 50, ExpiryDate = DateTime.Today.AddDays(60) },
            };
            var repo = CreateRepoMock(batches);
            var sut = new InventoryService(repo.Object);

            await sut.DeductStockFEFO(10, 1, 12, "ORDER-2");

            Assert.Equal(0, batches.Single(b => b.Id == 1).QuantityRemaining);
            Assert.Equal(43, batches.Single(b => b.Id == 2).QuantityRemaining); // 50 - (12 - 5)
        }

        [Fact]
        public async Task DeductStockFEFO_InsufficientTotalStock_ThrowsAndLeavesBatchesUntouched()
        {
            var batches = new List<StockBatch>
            {
                new() { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 5, ExpiryDate = DateTime.Today.AddDays(10) },
            };
            var repo = CreateRepoMock(batches);
            var sut = new InventoryService(repo.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeductStockFEFO(10, 1, 100, "ORDER-3"));

            Assert.Equal(5, batches.Single().QuantityRemaining); // không bị trừ oan khi thất bại
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task DeductStockFEFO_NonPositiveQuantity_ThrowsArgumentException(int quantity)
        {
            var repo = CreateRepoMock(new List<StockBatch>());
            var sut = new InventoryService(repo.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeductStockFEFO(10, 1, quantity, "ORDER-4"));
        }

        [Fact]
        public async Task RestoreStockFEFO_AddsBackToFirstActiveBatch()
        {
            var batches = new List<StockBatch>
            {
                new() { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 10, ExpiryDate = DateTime.Today.AddDays(10) },
            };
            var repo = CreateRepoMock(batches);
            var sut = new InventoryService(repo.Object);

            await sut.RestoreStockFEFO(10, 1, 7, "ORDER-5-RESTOCK");

            Assert.Equal(17, batches.Single().QuantityRemaining);
        }

        [Fact]
        public async Task RestoreStockFEFO_KnownOriginalReference_RestoresExactOriginalBatches()
        {
            // Lô A hết hạn xa hơn lô B nhưng đơn gốc đã xuất từ CẢ HAI lô (FEFO xuất lô B trước, rồi lô A).
            // Hoàn kho phải trả đúng về 2 lô đó theo đúng số lượng đã xuất, KHÔNG dồn hết vào lô hết hạn
            // sớm nhất hiện tại (khác hành vi mặc định khi không xác định được lô gốc).
            var batches = new List<StockBatch>
            {
                new() { Id = 1, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 40, ExpiryDate = DateTime.Today.AddDays(60) }, // Lô A
                new() { Id = 2, MedicineId = 10, WarehouseId = 1, QuantityRemaining = 0, ExpiryDate = DateTime.Today.AddDays(10) },  // Lô B (đã hết sau khi xuất)
            };
            var repo = CreateRepoMock(batches);
            repo.Setup(r => r.GetExportTransactionsForReference(10, 1, "ORDER-8"))
                .ReturnsAsync(new List<InventoryTransaction>
                {
                    new() { StockBatchId = 2, MedicineId = 10, WarehouseId = 1, Quantity = 5, CreatedAt = DateTime.Now.AddMinutes(-2) },
                    new() { StockBatchId = 1, MedicineId = 10, WarehouseId = 1, Quantity = 7, CreatedAt = DateTime.Now.AddMinutes(-1) },
                });

            var sut = new InventoryService(repo.Object);

            await sut.RestoreStockFEFO(10, 1, 12, "ORDER-8-RESTOCK");

            Assert.Equal(47, batches.Single(b => b.Id == 1).QuantityRemaining); // 40 + 7
            Assert.Equal(5, batches.Single(b => b.Id == 2).QuantityRemaining);  // 0 + 5
        }

        [Fact]
        public async Task RestoreStockFEFO_NoActiveBatch_CreatesReturnBatch()
        {
            var batches = new List<StockBatch>(); // không còn lô nào active (hết hạn/đã hủy hết)
            var repo = CreateRepoMock(batches);
            StockBatch? createdBatch = null;
            repo.Setup(r => r.AddBatch(It.IsAny<StockBatch>()))
                .ReturnsAsync((StockBatch b) => { b.Id = 99; createdBatch = b; return b; });

            var sut = new InventoryService(repo.Object);

            await sut.RestoreStockFEFO(10, 1, 8, "ORDER-6-RESTOCK");

            repo.Verify(r => r.AddBatch(It.Is<StockBatch>(b => b.MedicineId == 10 && b.WarehouseId == 1)), Times.Once);
            Assert.NotNull(createdBatch);
            Assert.Equal(8, createdBatch!.QuantityRemaining);
            Assert.Equal(StockBatchStatus.Active, createdBatch.Status);
        }

        [Fact]
        public async Task RestoreStockFEFO_ZeroQuantity_IsNoOp()
        {
            var repo = CreateRepoMock(new List<StockBatch>());
            var sut = new InventoryService(repo.Object);

            await sut.RestoreStockFEFO(10, 1, 0, "ORDER-7-RESTOCK");

            repo.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }
    }
}
