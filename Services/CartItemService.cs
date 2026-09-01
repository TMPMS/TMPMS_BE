using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartRepository _repo;
        public CartItemService(ICartRepository repo) => _repo = repo;

        public async Task<Cart?> GetOwnedCartAsync(int cartId, int currentUserId, bool canProxy)
        {
            var cart = await _repo.GetCartByIdAsync(cartId);
            if (cart == null) return null;
            if (!canProxy && cart.UserId != currentUserId) return null;
            return cart;
        }

        public Task<CartItem?> GetItemByIdAsync(int itemId) => _repo.GetCartItemByIdAsync(itemId);

        public async Task<List<CartItemViewDto>> GetItemsAsync(Cart cart)
        {
            var items = await _repo.GetCartItemsWithMedicineAsync(cart.Id);
            var result = new List<CartItemViewDto>();
            foreach (var ci in items)
            {
                int? allowedQuantity = null;
                if (ci.Medicine.RequiresPrescription)
                {
                    var (_, maxAllowed, _, _) = await GetRxAllowanceAsync(cart.UserId, ci.MedicineId);
                    allowedQuantity = Math.Max(0, maxAllowed);
                }
                result.Add(new CartItemViewDto
                {
                    Id = ci.Id,
                    CartId = ci.CartId,
                    MedicineId = ci.MedicineId,
                    Quantity = ci.Quantity,
                    AllowedQuantity = allowedQuantity,
                    // DTO thu hẹp thủ công (không gán thẳng entity ci.Medicine) — entity Medicine mang theo
                    // navigation property tới CartItem -> Cart -> User, khiến System.Text.Json serialize lồng
                    // cả PasswordHash/SecurityStamp của user ra response JSON của API giỏ hàng.
                    Medicine = new MedicineListItemDto
                    {
                        Id = ci.Medicine.Id,
                        CategoryId = ci.Medicine.CategoryId,
                        SupplierId = ci.Medicine.SupplierId,
                        Name = ci.Medicine.Name,
                        Description = ci.Medicine.Description,
                        Price = ci.Medicine.Price,
                        PriceStatus = ci.Medicine.Price == null ? "contact" : "available",
                        StockQuantity = ci.Medicine.StockQuantity,
                        ManufactureDate = ci.Medicine.ManufactureDate,
                        ExpiryDate = ci.Medicine.ExpiryDate,
                        RequiresPrescription = ci.Medicine.RequiresPrescription,
                        ImageUrl = ci.Medicine.ImageUrl,
                        Unit = ci.Medicine.Unit,
                        Origin = ci.Medicine.Origin,
                        Packaging = ci.Medicine.Packaging,
                        Barcode = ci.Medicine.Barcode,
                        OldPrice = ci.Medicine.OldPrice,
                        Discount = ci.Medicine.Discount,
                        IsActive = ci.Medicine.IsActive,
                        CreatedAt = ci.Medicine.CreatedAt
                    }
                });
            }
            return result;
        }

        public async Task<CartItemActionResult> AddItemAsync(Cart cart, int medicineId, int quantity)
        {
            var medicine = await _repo.GetMedicineByIdAsync(medicineId);
            if (medicine == null || medicine.Price == null)
            {
                return new CartItemActionResult { Success = false, Error = "Vị thuốc này chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." };
            }

            if (medicine.RequiresPrescription)
            {
                var (allowed, maxAllowed, prescribed, purchased) = await GetRxAllowanceAsync(cart.UserId, medicineId);
                if (!allowed)
                {
                    return new CartItemActionResult { Success = false, RequiresPrescription = true, Error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." };
                }
                if (maxAllowed <= 0)
                {
                    return new CartItemActionResult { Success = false, Error = $"Mỗi khách hàng chỉ được mua đúng số lượng thuốc mà bác sĩ đã kê đơn. Thuốc \"{medicine.Name}\": toa cho phép {prescribed}, bạn đã đặt {purchased} trong các đơn chưa hủy nên không thể mua thêm. Vui lòng hủy một đơn đang xử lý hoặc nhờ Dược sĩ điều chỉnh đơn thuốc." };
                }
                var existingForRx = await _repo.FindCartItemAsync(cart.Id, medicineId);
                var wouldHave = (existingForRx?.Quantity ?? 0) + quantity;
                if (wouldHave > maxAllowed)
                {
                    return new CartItemActionResult { Success = false, Error = $"Mỗi khách hàng chỉ được mua đúng số lượng thuốc mà bác sĩ đã kê đơn. Thuốc \"{medicine.Name}\": toa cho phép {prescribed}, bạn còn được mua {maxAllowed}. Vui lòng giảm số lượng hoặc nhờ Dược sĩ điều chỉnh đơn thuốc." };
                }
            }

            var existing = await _repo.FindCartItemAsync(cart.Id, medicineId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                await _repo.SaveChangesAsync();
                return new CartItemActionResult { Success = true, Item = ToBriefDto(existing), Created = false };
            }

            var newItem = new CartItem { CartId = cart.Id, MedicineId = medicineId, Quantity = quantity };
            await _repo.AddCartItemAsync(newItem);
            return new CartItemActionResult { Success = true, Item = ToBriefDto(newItem), Created = true };
        }

        private static CartItemBriefDto ToBriefDto(CartItem item) => new()
        {
            Id = item.Id,
            CartId = item.CartId,
            MedicineId = item.MedicineId,
            Quantity = item.Quantity
        };

        public async Task<CartItemActionResult> UpdateItemAsync(Cart cart, CartItem item, int quantity)
        {
            if (item.Quantity != quantity)
            {
                var medicine = await _repo.GetMedicineByIdAsync(item.MedicineId);
                if (medicine != null && medicine.RequiresPrescription)
                {
                    var (allowed, maxAllowed, prescribed, purchased) = await GetRxAllowanceAsync(cart.UserId, item.MedicineId);
                    if (!allowed)
                    {
                        return new CartItemActionResult { Success = false, RequiresPrescription = true, Error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." };
                    }
                    if (quantity > maxAllowed)
                    {
                        return new CartItemActionResult { Success = false, Error = $"Mỗi khách hàng chỉ được mua đúng số lượng thuốc mà bác sĩ đã kê đơn. Thuốc \"{medicine.Name}\": toa cho phép {prescribed}, bạn đã đặt {purchased} và chỉ còn được mua {maxAllowed}. Vui lòng giảm số lượng hoặc nhờ Dược sĩ điều chỉnh đơn thuốc." };
                    }
                }
            }

            item.Quantity = quantity;
            await _repo.SaveChangesAsync();
            return new CartItemActionResult { Success = true, Item = ToBriefDto(item) };
        }

        public async Task RemoveItemAsync(CartItem item)
        {
            await _repo.RemoveCartItemAsync(item);
        }

        public async Task<CartSyncResult> SyncAsync(int userId, List<CartSyncItemInput> items)
        {
            var cart = await _repo.GetCartByUserIdSingleAsync(userId);
            if (cart == null)
            {
                cart = await _repo.CreateCartAsync(new Cart { UserId = userId });
            }

            foreach (var item in items)
            {
                var medicine = await _repo.GetMedicineByIdAsync(item.MedicineId);
                if (medicine == null || medicine.Price == null)
                {
                    return new CartSyncResult { Success = false, Error = $"Vị thuốc '{medicine?.Name ?? item.MedicineId.ToString()}' chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." };
                }

                if (medicine.RequiresPrescription)
                {
                    var (allowed, maxAllowed, prescribed, purchased) = await GetRxAllowanceAsync(userId, item.MedicineId);
                    if (!allowed)
                    {
                        return new CartSyncResult { Success = false, RequiresPrescription = true, Error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." };
                    }
                    if (item.Quantity > maxAllowed)
                    {
                        return new CartSyncResult { Success = false, Error = $"Mỗi khách hàng chỉ được mua đúng số lượng thuốc mà bác sĩ đã kê đơn. Thuốc \"{medicine.Name}\": toa cho phép {prescribed}, bạn đã đặt {purchased} và chỉ còn được mua {maxAllowed}. Vui lòng giảm số lượng hoặc nhờ Dược sĩ điều chỉnh đơn thuốc." };
                    }
                }

                var existing = await _repo.FindCartItemAsync(cart.Id, item.MedicineId);
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    await _repo.AddCartItemAsync(new CartItem { CartId = cart.Id, MedicineId = item.MedicineId, Quantity = item.Quantity });
                }
            }

            await _repo.SaveChangesAsync();
            return new CartSyncResult { Success = true, Cart = cart };
        }

        // Returns (Allowed, MaxAllowed, Prescribed, Purchased):
        // Allowed = user has an Approved/Fulfilled prescription containing the medicine.
        // MaxAllowed = total prescribed quantity minus what has already been purchased
        // in non-cancelled orders (does NOT subtract the current cart quantity).
        // Prescribed/Purchased: số liệu chi tiết để FE thông báo rõ ràng cho user.
        private async Task<(bool Allowed, int MaxAllowed, int Prescribed, int Purchased)> GetRxAllowanceAsync(int userId, int medicineId)
        {
            var prescribed = await _repo.GetPrescribedQuantityAsync(userId, medicineId);
            if (prescribed <= 0) return (false, 0, 0, 0);

            var purchased = await _repo.GetPurchasedQuantityAsync(userId, medicineId);
            return (true, Math.Max(0, prescribed - purchased), prescribed, purchased);
        }
    }
}
