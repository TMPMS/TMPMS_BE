using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class PrescriptionItem
    {
        public int Id { get; set; }

        public int PrescriptionId { get; set; }

        public int MedicineId { get; set; }

        public int Quantity { get; set; }

        // Hướng dẫn sử dụng do Dược sĩ ghi khi kê đơn (vd: "Sáng 1 viên - Tối 1 viên, sau ăn")
        public string? Instructions { get; set; }

        public Prescription Prescription { get; set; }

        public Medicine Medicine { get; set; }
    }
}
