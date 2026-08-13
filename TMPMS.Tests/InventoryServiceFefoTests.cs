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
