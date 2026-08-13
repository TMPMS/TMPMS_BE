using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IMedicineService
    {
        Task<(List<MedicineListItemDto> Items, int TotalCount, bool IsPaged)> SearchAsync(MedicineSearchFilterDto filter);
        Task<HerbalFilterOptionsDto> GetHerbalFilterOptionsAsync();
        Task<MedicineDetailDto?> GetByIdAsync(int id);
        Task<MedicineBarcodeDto?> GetByBarcodeAsync(string barcode);
        Task<Medicine> CreateAsync(MedicineCreateDto dto);
        Task<MedicineUpdateResponseDto?> UpdateAsync(int id, MedicineUpdateDto dto);
        Task<(bool Found, bool Deactivated, string? Name)> DeleteAsync(int id);
    }
}
