using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Medicine
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public int SupplierId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal? Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool RequiresPrescription { get; set; }

        public string ImageUrl { get; set; }

        public string? Unit { get; set; }

        public string? Origin { get; set; }

        public string? Packaging { get; set; }

        public decimal? OldPrice { get; set; }

        public int? Discount { get; set; }

        public DateTime CreatedAt { get; set; }

        public Category Category { get; set; }

        public Supplier Supplier { get; set; }

        public ICollection<CartItem> CartItems { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }

        public ICollection<Review> Reviews { get; set; }

        public ICollection<MedicineImage> Images { get; set; }

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; }

        public ICollection<InventoryStock> InventoryStocks { get; set; }
    }
}
