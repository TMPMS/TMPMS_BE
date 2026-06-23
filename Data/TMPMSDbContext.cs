using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TMPMS.Data
{
    public class TMPMSDbContext : DbContext
    {
        public TMPMSDbContext()
        {
        }

        public TMPMSDbContext(DbContextOptions<TMPMSDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

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
                .HasForeignKey(p => p.UserId);

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
        }
    }
}
