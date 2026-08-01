using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repo;
        public PrescriptionService(IPrescriptionRepository repo) => _repo = repo;

        public async Task<PrescriptionResponseDTO> Create(PrescriptionCreateDTO dto)
        {
            var entity = new Prescription
            {
                UserId = dto.UserId,
                DiagnosisId = dto.DiagnosisId,
                DoctorId = dto.DoctorId,
                DoctorName = dto.DoctorName,
                Hospital = dto.Hospital,
                PrescriptionDate = dto.PrescriptionDate == default ? DateTime.Now : dto.PrescriptionDate,
                ImageUrl = dto.ImageUrl,
                Status = "Pending",
                PrescriptionItems = dto.Items.Select(i => new PrescriptionItem
                {
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity
                }).ToList()
            };

            var created = await _repo.Create(entity);
            var full = await _repo.GetById(created.Id);
            return Map(full);
        }

        public async Task<PrescriptionResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<PrescriptionResponseDTO>> GetByUser(int userId)
        {
            var list = await _repo.GetByUser(userId);
            return list.Select(Map).ToList();
        }

        public async Task<List<PrescriptionResponseDTO>> GetByStatus(string status)
        {
            var list = await _repo.GetByStatus(status);
            return list.Select(Map).ToList();
        }

        public async Task<List<PrescriptionResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(Map).ToList();
        }

        // Duyệt / Từ chối đơn thuốc. Chỉ những thuốc RequiresPrescription mới cần đơn hợp lệ để mua.
        public async Task<PrescriptionResponseDTO> UpdateStatus(int id, PrescriptionStatusUpdateDTO dto)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;

            var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Fulfilled" };
            if (!allowedStatuses.Contains(dto.Status))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            entity.Status = dto.Status;
            var updated = await _repo.Update(entity);
            return Map(updated);
        }

        public async Task<bool> Delete(int id) => await _repo.Delete(id);

        private PrescriptionResponseDTO Map(Prescription p)
        {
            return new PrescriptionResponseDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User?.UserName,
                DiagnosisId = p.DiagnosisId,
                DoctorId = p.DoctorId,
                DoctorName = p.DoctorName ?? p.Doctor?.UserName,
                Hospital = p.Hospital,
                PrescriptionDate = p.PrescriptionDate,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                Items = p.PrescriptionItems?.Select(i => new PrescriptionItemResponseDTO
                {
                    Id = i.Id,
                    MedicineId = i.MedicineId,
                    MedicineName = i.Medicine?.Name,
                    Quantity = i.Quantity,
                    RequiresPrescription = i.Medicine?.RequiresPrescription ?? false
                }).ToList() ?? new List<PrescriptionItemResponseDTO>()
            };
        }

    }
}
