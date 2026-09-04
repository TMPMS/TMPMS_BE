using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Method { get; set; }

        public string TransactionCode { get; set; }

        /// <summary>
        /// Mã orderCode thật đã gửi cho PayOS khi tạo link thanh toán — KHÔNG dùng chung với OrderId,
        /// vì PayOS nhớ orderCode đã dùng theo tài khoản (ClientId), không phải theo DB local: nếu dùng
        /// thẳng OrderId (số nhỏ, tăng tuần tự) thì sau khi DB local bị reset/seed lại, một đơn hoàn
        /// toàn mới có thể trùng orderCode một request PayOS thật đã từng nhận trước đó và bị từ chối
        /// là trùng. Null cho đến khi CreatePaymentLink được gọi lần đầu.
        /// </summary>
        public long? PayOsOrderCode { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public Order Order { get; set; }
    }
}
