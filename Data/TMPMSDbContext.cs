using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPMS.Models;

namespace TMPMS.Data
{
    public class TMPMSDbContext : IdentityDbContext<User, Role, int,
        IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>>

    {
        public TMPMSDbContext()
        {
        }

        public TMPMSDbContext(DbContextOptions<TMPMSDbContext> options) : base(options) { }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<MedicineImage> MedicineImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierMedicine> SupplierMedicines { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryStock> InventoryStocks { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<StockBatch> StockBatches { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        public DbSet<Diagnosis> Diagnoses { get; set; }
        public DbSet<HerbalMedicineInfo> HerbalMedicineInfos { get; set; }
        public DbSet<HerbalInteraction> HerbalInteractions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentSlotHold> AppointmentSlotHolds { get; set; }
        public DbSet<AppointmentPayment> AppointmentPayments { get; set; }
        public DbSet<AppointmentRescheduleRequest> AppointmentRescheduleRequests { get; set; }
        public DbSet<AppointmentPaymentIntent> AppointmentPaymentIntents { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<WheelSpin> WheelSpins { get; set; }
        public DbSet<SymptomQuestion> SymptomQuestions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<SyndromeType> SyndromeTypes { get; set; }
        public DbSet<AnswerScoreMapping> AnswerScoreMappings { get; set; }
        public DbSet<DiagnosisAnswer> DiagnosisAnswers { get; set; }
        public DbSet<PharmacyChatSession> PharmacyChatSessions { get; set; }
        public DbSet<PharmacyChatMessage> PharmacyChatMessages { get; set; }
        public DbSet<PharmacyStore> Stores { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }

        public DbSet<HealthQuiz> HealthQuizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizAnswerOption> QuizAnswerOptions { get; set; }
        public DbSet<QuizResultBand> QuizResultBands { get; set; }
        public DbSet<QuizSession> QuizSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Các cột lưu thời điểm (instant) theo UTC: đánh dấu lại Kind=Utc khi đọc
            // để JSON trả về kèm "Z" (FE quy đổi sang giờ địa phương thay vì in thô UTC).
            modelBuilder.Entity<Order>()
                .Property(o => o.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);
            modelBuilder.Entity<Appointment>()
                .Property(a => a.ConfirmationDeadline).HasConversion(UtcDateTimeValueConverter.Instance);
            modelBuilder.Entity<Appointment>()
                .Property(a => a.ConfirmedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<AppointmentSlotHold>().HasIndex(h => h.Token).IsUnique();
            modelBuilder.Entity<AppointmentSlotHold>().Property(h => h.Location).HasMaxLength(450);
            modelBuilder.Entity<Appointment>().Property(a => a.SymptomDescription).HasMaxLength(500);
            modelBuilder.Entity<AppointmentPaymentIntent>().HasIndex(i => i.OrderCode).IsUnique();
            modelBuilder.Entity<AppointmentPaymentIntent>()
                .HasOne(i => i.SlotHold).WithMany().HasForeignKey(i => i.SlotHoldId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppointmentSlotHold>().HasIndex(h => new { h.AppointmentDate, h.Location });
            modelBuilder.Entity<AppointmentPayment>()
                .HasOne(p => p.Appointment).WithMany(a => a.AppointmentPayments)
                .HasForeignKey(p => p.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AppointmentRescheduleRequest>()
                .HasOne(r => r.Appointment).WithMany(a => a.RescheduleRequests)
                .HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .Property(r => r.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<QuizSession>()
                .Property(q => q.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<Voucher>()
                .Property(v => v.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.Code)
                .IsUnique();

            modelBuilder.Entity<Voucher>()
                .Property(v => v.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<WheelSpin>()
                .Property(w => w.SpinDate).HasConversion(UtcDateTimeValueConverter.Instance);
            modelBuilder.Entity<WheelSpin>()
                .Property(w => w.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<WheelSpin>()
                .HasIndex(w => new { w.UserId, w.SpinDate })
                .IsUnique();

            modelBuilder.Entity<WheelSpin>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WheelSpin>()
                .HasOne(w => w.Voucher)
                .WithMany()
                .HasForeignKey(w => w.VoucherId)
                .OnDelete(DeleteBehavior.SetNull);

            // Order -> Voucher: dùng Restrict (không SetNull) cho cả 2 FK vì SQL Server chặn
            // "multiple cascade paths" khi 2 cột cùng trỏ về 1 bảng với hành vi cascade/setnull
            // (giống lý do HerbalInteraction dùng Restrict ở trên).
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ProductVoucher)
                .WithMany()
                .HasForeignKey(o => o.ProductVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingVoucher)
                .WithMany()
                .HasForeignKey(o => o.ShippingVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Medicine>()
                .Property(m => m.CreatedAt).HasConversion(UtcDateTimeValueConverter.Instance);

            modelBuilder.Entity<HealthQuiz>()
                .HasIndex(q => q.Code)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart - User
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId);

            // CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Medicine)
                .WithMany(m => m.CartItems)
                .HasForeignKey(ci => ci.MedicineId);

            // Medicine
            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Medicines)
                .HasForeignKey(m => m.CategoryId);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Supplier)
                .WithMany()
                .HasForeignKey(m => m.SupplierId);

            // MedicineImage
            modelBuilder.Entity<MedicineImage>()
                .HasOne(mi => mi.Medicine)
                .WithMany(m => m.Images)
                .HasForeignKey(mi => mi.MedicineId);

            // Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ProcessedByStaff)
                .WithMany()
                .HasForeignKey(o => o.ProcessedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Medicine)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(oi => oi.MedicineId);

            // Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId);

            // Prescription
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.User)
                .WithMany(u => u.Prescriptions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Diagnosis)
                .WithMany(d => d.Prescriptions)
                .HasForeignKey(p => p.DiagnosisId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Appointment
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.ConfirmedBy)
                .WithMany()
                .HasForeignKey(a => a.ConfirmedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Diagnosis>()
                .HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Diagnosis>()
                .HasOne(d => d.Doctor)
                .WithMany()
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Diagnosis>()
                .HasOne(d => d.PrimarySyndrome)
                .WithMany()
                .HasForeignKey(d => d.PrimarySyndromeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Diagnosis>()
                .HasOne(d => d.SecondarySyndrome)
                .WithMany()
                .HasForeignKey(d => d.SecondarySyndromeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DiagnosisAnswer>()
                .HasOne(da => da.Diagnosis)
                .WithMany(d => d.DiagnosisAnswers)
                .HasForeignKey(da => da.DiagnosisId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiagnosisAnswer>()
                .HasOne(da => da.Question)
                .WithMany()
                .HasForeignKey(da => da.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DiagnosisAnswer>()
                .HasOne(da => da.AnswerOption)
                .WithMany()
                .HasForeignKey(da => da.AnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnswerScoreMapping>()
                .HasOne(asm => asm.AnswerOption)
                .WithMany(ao => ao.ScoreMappings)
                .HasForeignKey(asm => asm.AnswerOptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnswerScoreMapping>()
                .HasOne(asm => asm.SyndromeType)
                .WithMany()
                .HasForeignKey(asm => asm.SyndromeTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HerbalMedicineInfo>()
                .HasOne(h => h.Medicine)
                .WithOne()
                .HasForeignKey<HerbalMedicineInfo>(h => h.MedicineId);

            // HerbalInteraction — dùng Restrict cho mọi FK để tránh lỗi "multiple cascade paths"
            // (4 FK đều trỏ về Medicine).
            modelBuilder.Entity<HerbalInteraction>()
                .HasOne(hi => hi.HerbA)
                .WithMany()
                .HasForeignKey(hi => hi.HerbAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HerbalInteraction>()
                .HasOne(hi => hi.HerbB)
                .WithMany()
                .HasForeignKey(hi => hi.HerbBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HerbalInteraction>()
                .HasOne(hi => hi.SuggestedReplacementForA)
                .WithMany()
                .HasForeignKey(hi => hi.SuggestedReplacementForAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HerbalInteraction>()
                .HasOne(hi => hi.SuggestedReplacementForB)
                .WithMany()
                .HasForeignKey(hi => hi.SuggestedReplacementForBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HerbalInteraction>()
                .HasIndex(hi => new { hi.HerbAId, hi.HerbBId })
                .IsUnique();

            // PrescriptionItem
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId);

            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medicine)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(pi => pi.MedicineId);

            // Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Medicine)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MedicineId);

            // UserAddress
            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId);

            // InventoryStock
            modelBuilder.Entity<InventoryStock>()
                .HasKey(i => new { i.MedicineId, i.WarehouseId });

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId);

            // SupplierMedicine
            modelBuilder.Entity<SupplierMedicine>()
                .HasKey(sm => new { sm.SupplierId, sm.MedicineId });

            // InventoryTransaction
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(i => i.Medicine)
                .WithMany()
                .HasForeignKey(i => i.MedicineId);

            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId);            

            modelBuilder.Entity<InventoryStock>()
                .HasOne(x => x.Medicine)
                .WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.MedicineId);

            modelBuilder.Entity<InventoryStock>()
                .HasOne(x => x.Warehouse)
                .WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.WarehouseId);

            // StockBatch (lô hàng nhập kho — mỗi lô có hạn dùng/giá nhập riêng)
            // Dùng Restrict cho các FK để tránh lỗi "multiple cascade paths" của SQL Server
            // (Medicine/Warehouse/Supplier đều có thể dẫn tới cùng một StockBatch).
            modelBuilder.Entity<StockBatch>()
                .HasOne(b => b.Medicine)
                .WithMany(m => m.StockBatches)
                .HasForeignKey(b => b.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockBatch>()
                .HasOne(b => b.Warehouse)
                .WithMany(w => w.StockBatches)
                .HasForeignKey(b => b.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockBatch>()
                .HasOne(b => b.Supplier)
                .WithMany()
                .HasForeignKey(b => b.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockBatch>()
                .HasIndex(b => new { b.MedicineId, b.WarehouseId, b.BatchNumber })
                .IsUnique();

            modelBuilder.Entity<StockBatch>()
                .HasIndex(b => new { b.MedicineId, b.WarehouseId, b.ExpiryDate });

            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.StockBatch)
                .WithMany(b => b.InventoryTransactions)
                .HasForeignKey(t => t.StockBatchId)
                .OnDelete(DeleteBehavior.SetNull);

            // FlashSale — bảng quản lý lịch sử áp dụng/gỡ giảm giá hàng gần hết hạn.
            modelBuilder.Entity<FlashSale>()
                .HasOne(f => f.Medicine)
                .WithMany()
                .HasForeignKey(f => f.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FlashSale>()
                .HasOne(f => f.Batch)
                .WithMany()
                .HasForeignKey(f => f.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FlashSale>()
                .HasOne(f => f.AppliedByStaff)
                .WithMany()
                .HasForeignKey(f => f.AppliedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FlashSale>()
                .HasOne(f => f.RemovedByStaff)
                .WithMany()
                .HasForeignKey(f => f.RemovedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FlashSale>()
                .HasIndex(f => new { f.MedicineId, f.IsActive });

            //modelBuilder.Entity<SupplierMedicine>()
            //    .HasOne(x => x.Supplier)
            //    .WithMany(x => x.SupplierMedicines)
            //    .HasForeignKey(x => x.SupplierId);

            //modelBuilder.Entity<SupplierMedicine>()
            //    .HasOne(x => x.Medicine)
            //    .WithMany()
            //    .HasForeignKey(x => x.MedicineId);
            modelBuilder.Entity<SupplierMedicine>()
                .HasOne(sm => sm.Supplier)
                .WithMany(s => s.SupplierMedicines)
                .HasForeignKey(sm => sm.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SupplierMedicine>()
                .HasOne(sm => sm.Medicine)
                .WithMany()
                .HasForeignKey(sm => sm.MedicineId)
                .OnDelete(DeleteBehavior.NoAction);

            // Pharmacy Chat relationships
            modelBuilder.Entity<PharmacyChatSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PharmacyChatSession>()
                .HasOne(s => s.AssignedPharmacist)
                .WithMany()
                .HasForeignKey(s => s.AssignedPharmacistId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PharmacyChatMessage>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PharmacyChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mỗi GoogleId chỉ liên kết đúng 1 tài khoản; NULL trùng nhau thì được phép
            modelBuilder.Entity<User>()
                .HasIndex(u => u.GoogleId)
                .IsUnique()
                .HasFilter("[GoogleId] IS NOT NULL");
        }
    }
}
