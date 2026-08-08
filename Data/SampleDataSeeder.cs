using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Models;

namespace TMPMS.Data
{
    public static class SampleDataSeeder
    {
        public static async Task SeedAsync(TMPMSDbContext context)
        {
            // 1. Ensure Diverse Real Customer Users exist with real Vietnamese FullName
            var users = await context.Users.ToListAsync();
            var customerProfiles = new[]
            {
                ("khachhang1@gmail.com", "Nguyễn Văn Hùng", "0988123456", "Số 15 Lê Văn Lương, Cầu Giấy, Hà Nội"),
                ("khachhang2@gmail.com", "Cô Trần Thị Mai", "0912345678", "24 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội"),
                ("khachhang3@gmail.com", "Thầy thuốc Lê Hoàng Nam", "0977889900", "188 Đường 3 Tháng 2, Hải Châu, Đà Nẵng"),
                ("khachhang4@gmail.com", "Phạm Minh Đức", "0934567890", "85 Trần Hưng Đạo, Quận 1, TP. Hồ Chí Minh"),
                ("khachhang5@gmail.com", "Đỗ Thu Hà", "0905112233", "54 Chùa Bộc, Đống Đa, Hà Nội"),
                ("khachhang6@gmail.com", "Vũ Quốc Anh", "0944556677", "9 Hai Bà Trưng, Hoàn Kiếm, Hà Nội"),
                ("khachhang7@gmail.com", "Nguyễn Thị Ngọc Anh", "0966778899", "102 Cầu Giấy, Hà Nội"),
                ("khachhang8@gmail.com", "Bác sĩ Bùi Văn Sang", "0922334455", "Viện Y Học Cổ Truyền Việt Nam")
            };

            foreach (var prof in customerProfiles)
            {
                var existingUser = users.FirstOrDefault(u => u.Email == prof.Item1 || u.UserName == prof.Item1);
                if (existingUser != null)
                {
                    existingUser.FullName = prof.Item2;
                    existingUser.PhoneNumber = prof.Item3;
                    existingUser.Address = prof.Item4;
                }
                else
                {
                    var newUser = new User
                    {
                        UserName = prof.Item1,
                        Email = prof.Item1,
                        FullName = prof.Item2,
                        PhoneNumber = prof.Item3,
                        Address = prof.Item4,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    };
                    context.Users.Add(newUser);
                }
            }
            await context.SaveChangesAsync();
            users = await context.Users.ToListAsync();

            // 2. Seed Sample Vouchers if empty
            if (!await context.Vouchers.AnyAsync())
            {
                context.Vouchers.AddRange(
                    new Voucher
                    {
                        Code = "THAIMINH50",
                        Name = "Voucher Giảm 50k Thái Minh",
                        DiscountType = "flat",
                        DiscountValue = 50000,
                        MinOrderValue = 300000,
                        MaxDiscount = 50000,
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        EndDate = DateTime.UtcNow.AddDays(90),
                        UsageLimit = 100,
                        UsedCount = 12,
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "LONGCHAU10",
                        Name = "Voucher Tri Ân 10%",
                        DiscountType = "percent",
                        DiscountValue = 10,
                        MinOrderValue = 200000,
                        MaxDiscount = 100000,
                        StartDate = DateTime.UtcNow.AddDays(-15),
                        EndDate = DateTime.UtcNow.AddDays(60),
                        UsageLimit = 200,
                        UsedCount = 45,
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "FREESHIPGSP",
                        Name = "Miễn Phí Vận Chuyển GSP",
                        DiscountType = "flat",
                        DiscountValue = 40000,
                        MinOrderValue = 150000,
                        MaxDiscount = 40000,
                        StartDate = DateTime.UtcNow.AddDays(-60),
                        EndDate = DateTime.UtcNow.AddDays(120),
                        UsageLimit = 500,
                        UsedCount = 128,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. Seed Realistic Orders across different dates
            if (await context.Orders.CountAsync() < 10)
            {
                var medicines = await context.Medicines.Take(12).ToListAsync();
                if (medicines.Any())
                {
                    var random = new Random(42);
                    var customerUsers = users.Where(u => u.Email != null && u.Email.Contains("khachhang")).ToList();
                    if (!customerUsers.Any()) customerUsers = users;

                    var statuses = new[] { "Delivered", "Delivered", "Delivered", "Shipping", "Pending", "Delivered" };
                    var paymentMethods = new[] { "PAYOS", "COD", "PAYOS", "COD", "PAYOS" };

                    var now = DateTime.UtcNow;

                    for (int i = 0; i < 15; i++)
                    {
                        var createdDaysAgo = i * 2;
                        var createdDate = now.AddDays(-createdDaysAgo);
                        var targetUser = customerUsers[i % customerUsers.Count];
                        var med1 = medicines[i % medicines.Count];
                        var med2 = medicines[(i + 3) % medicines.Count];

                        var price1 = med1.Price ?? 0m;
                        var price2 = med2.Price ?? 0m;
                        var qty1 = random.Next(1, 4);
                        var qty2 = random.Next(1, 3);
                        var subtotal = (price1 * qty1) + (price2 * qty2);
                        var shippingFee = 40000m;
                        var totalAmount = subtotal + shippingFee;

                        var orderStatus = statuses[i % statuses.Length];
                        var payMethod = paymentMethods[i % paymentMethods.Length];

                        var order = new Order
                        {
                            UserId = targetUser.Id,
                            ShippingAddress = $"{targetUser.FullName} - {targetUser.PhoneNumber} - {targetUser.Address}",
                            TotalAmount = totalAmount,
                            ShippingFee = shippingFee,
                            PaymentStatus = orderStatus == "Delivered" || payMethod == "PAYOS" ? "Paid" : "Pending",
                            Status = orderStatus,
                            CreatedAt = createdDate,
                            OrderItems = new List<OrderItem>
                            {
                                new OrderItem
                                {
                                    MedicineId = med1.Id,
                                    Quantity = qty1,
                                    Price = price1
                                },
                                new OrderItem
                                {
                                    MedicineId = med2.Id,
                                    Quantity = qty2,
                                    Price = price2
                                }
                            }
                        };

                        context.Orders.Add(order);
                        await context.SaveChangesAsync();

                        if (order.PaymentStatus == "Paid")
                        {
                            context.Payments.Add(new Payment
                            {
                                OrderId = order.Id,
                                Amount = order.TotalAmount,
                                Method = payMethod,
                                Status = "Completed",
                                TransactionCode = $"PAY-{order.Id}-{DateTime.Now.Ticks % 100000}",
                                PaidAt = createdDate.AddMinutes(15)
                            });

                            context.Invoices.Add(new Invoice
                            {
                                OrderId = order.Id,
                                InvoiceCode = $"INV-2026-00{order.Id}",
                                TotalAmount = order.TotalAmount,
                                IssuedAt = createdDate.AddMinutes(20)
                            });

                            await context.SaveChangesAsync();
                        }
                    }
                }
            }

            // 4. Seed Diverse Authentic Product Reviews (Delete old sparse reviews & re-seed rich reviews for all medicines)
            var currentReviewsCount = await context.Reviews.CountAsync();
            if (currentReviewsCount < 20)
            {
                // Clear existing sparse reviews to replace with high quality authentic reviews
                var oldReviews = await context.Reviews.ToListAsync();
                context.Reviews.RemoveRange(oldReviews);
                await context.SaveChangesAsync();

                var allMedicines = await context.Medicines.ToListAsync();
                var customerUsers = users.Where(u => u.Email != null && u.Email.Contains("khachhang")).ToList();
                if (!customerUsers.Any()) customerUsers = users;

                var realisticReviewPool = new (int Rating, string Comment)[]
                {
                    (5, "Trà túi lọc uống rất thơm và mát gan. Tôi dùng liên tục 2 tuần thấy da dẻ mát mẻ, hết hẳn ngứa dị ứng! Đóng gói chuẩn GSP có tem niêm phong."),
                    (5, "Cao xương khớp này tuyệt vời lắm! Mẹ tôi 65 tuổi bị thoái hóa khớp gối uống 1 lọ đã thấy bớt đau nhức hẳn, đi lại dễ dàng hơn."),
                    (5, "Mật ong rừng thơm lừng, sánh mịn nguyên chất 100%. Pha với nước ấm uống buổi sáng cảm giác rất dễ chịu bụng."),
                    (5, "Bột gừng mật ong sấy thăng hoa thơm ngon ấm bụng vô cùng! Những ngày rét mùa đông pha 1 gói là ấm người ngay, giảm ho rõ rệt."),
                    (5, "Đông trùng hạ thảo militaris sợi vàng óng, nguyên con rất đẹp. Tôi ngâm mật ong với rượu uống tăng cường thể lực cực kỳ hiệu quả."),
                    (5, "Cao Atiso Đà Lạt chuẩn nguyên chất, vị đắng ngọt dịu rất dễ uống. Hỗ trợ giải độc gan sau những ngày nhậu hiệu quả rõ rệt."),
                    (5, "Nhân sâm lát tẩm mật ong KGC ngậm mỗi sáng rất ngậy và tỉnh táo làm việc cả ngày. Hàng Hàn Quốc chính hãng đóng hộp sang trọng."),
                    (4, "Viên nghệ mật ong sữa chúa thơm ngon dễ ngậm, uống trước bữa ăn 30 phút thấy êm dạ dày hẳn, giảm trào ngược dạ dày."),
                    (5, "Dầu tràm nguyên chất Cung Đình thơm nức, giữ ấm cho bé ban đêm rất an toàn. Nhà thuốc tư vấn chu đáo và giao hàng siêu tốc 2 tiếng!")
                };

                int reviewCounter = 0;
                foreach (var med in allMedicines)
                {
                    // Add 2-3 authentic reviews per medicine
                    int countForThisMed = 2 + (med.Id % 2);
                    for (int k = 0; k < countForThisMed; k++)
                    {
                        var userObj = customerUsers[(reviewCounter + k) % customerUsers.Count];
                        var itemData = realisticReviewPool[(reviewCounter + k) % realisticReviewPool.Length];

                        context.Reviews.Add(new Review
                        {
                            MedicineId = med.Id,
                            UserId = userObj.Id,
                            Rating = itemData.Rating,
                            Comment = itemData.Comment,
                            CreatedAt = DateTime.UtcNow.AddDays(-(reviewCounter % 20) - 1)
                        });
                        reviewCounter++;
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
