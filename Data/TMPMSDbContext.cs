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
        public DbSet<Diagnosis> Diagnoses { get; set; }
        public DbSet<HerbalMedicineInfo> HerbalMedicineInfos { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<SymptomQuestion> SymptomQuestions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<SyndromeType> SyndromeTypes { get; set; }
        public DbSet<AnswerScoreMapping> AnswerScoreMappings { get; set; }
        public DbSet<DiagnosisAnswer> DiagnosisAnswers { get; set; }
        public DbSet<PharmacyChatSession> PharmacyChatSessions { get; set; }
        public DbSet<PharmacyChatMessage> PharmacyChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Diagnosis)
                .WithMany(d => d.Prescriptions)
                .HasForeignKey(p => p.DiagnosisId)
                .OnDelete(DeleteBehavior.SetNull);

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
        }
    }
}
