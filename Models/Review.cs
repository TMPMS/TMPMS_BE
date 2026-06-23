using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int MedicineId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; }

        public Medicine Medicine { get; set; }
    }
}
