using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class HerbalMedicineService : IHerbalMedicineService
    {
        private readonly IHerbalMedicineRepository _repo;
        public HerbalMedicineService(IHerbalMedicineRepository repo) => _repo = repo;

        public async Task<HerbalMedicineResponseDTO> Create(HerbalMedicineCreateDTO dto)
        {
            var medicine = new Medicine
            {
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ManufactureDate = dto.ManufactureDate,
                ExpiryDate = dto.ExpiryDate,
                RequiresPrescription = false,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.Now
            };
            var createdMedicine = await _repo.CreateMedicine(medicine);

            var herbalInfo = new HerbalMedicineInfo
            {
                MedicineId = createdMedicine.Id,
                OriginPlace = dto.OriginPlace,
                PartUsed = dto.PartUsed,
                Properties = dto.Properties,
                Effects = dto.Effects,
                UsageInstructions = dto.UsageInstructions,
                Dosage = dto.Dosage,
                Contraindications = dto.Contraindications,
                PreservationMethod = dto.PreservationMethod
            };
            await _repo.CreateHerbalInfo(herbalInfo);

            var full = await _repo.GetHerbalInfoByMedicineId(createdMedicine.Id);
            return Map(full);
        }

        public async Task<HerbalMedicineResponseDTO> GetByMedicineId(int medicineId)
        {
            var info = await _repo.GetHerbalInfoByMedicineId(medicineId);
            return info == null ? null : Map(info);
        }

        public async Task<List<HerbalMedicineResponseDTO>> GetAll()
        {
            var list = await _repo.GetAllHerbal();
            return list.Select(Map).ToList();
        }

        public async Task<HerbalMedicineResponseDTO> Update(int medicineId, HerbalMedicineUpdateDTO dto)
        {
            var info = await _repo.GetHerbalInfoByMedicineId(medicineId);
            if (info == null) return null;

            if (info.Medicine != null)
            {
                info.Medicine.Name = dto.Name ?? info.Medicine.Name;
                info.Medicine.Description = dto.Description ?? info.Medicine.Description;
                if (dto.Price > 0) info.Medicine.Price = dto.Price;
            }

            info.OriginPlace = dto.OriginPlace ?? info.OriginPlace;
            info.PartUsed = dto.PartUsed ?? info.PartUsed;
            info.Properties = dto.Properties ?? info.Properties;
            info.Effects = dto.Effects ?? info.Effects;
            info.UsageInstructions = dto.UsageInstructions ?? info.UsageInstructions;
            info.Dosage = dto.Dosage ?? info.Dosage;
            info.Contraindications = dto.Contraindications ?? info.Contraindications;
            info.PreservationMethod = dto.PreservationMethod ?? info.PreservationMethod;

            var updated = await _repo.Update(info);
            return Map(updated);
        }

        public async Task<bool> Delete(int medicineId) => await _repo.Delete(medicineId);

        private HerbalMedicineResponseDTO Map(HerbalMedicineInfo info)
        {
            return new HerbalMedicineResponseDTO
            {
                MedicineId = info.MedicineId ?? 0,
                Name = info.Medicine?.Name,
                Description = info.Medicine?.Description,
                Price = info.Medicine?.Price ?? 0,
                StockQuantity = info.Medicine?.StockQuantity ?? 0,
                ImageUrl = info.Medicine?.ImageUrl,
                CategoryName = info.Medicine?.Category?.Name,
                OriginPlace = info.OriginPlace,
                PartUsed = info.PartUsed,
                Properties = info.Properties,
                Effects = info.Effects,
                UsageInstructions = info.UsageInstructions,
                Dosage = info.Dosage,
                Contraindications = info.Contraindications,
                PreservationMethod = info.PreservationMethod
            };
        }
    }
}
