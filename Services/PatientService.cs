using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Repositories.Interfaces;
using TMPMS.DTOs;
using TMPMS.Repositories;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly IPrescriptionItemRepository _prescriptionItemRepository;


        public PatientService(IPatientRepository patientRepository,
            IAddressRepository addressRepository,
    IDiagnosisRepository diagnosisRepository,
    IPrescriptionRepository prescriptionRepository,
    IPrescriptionItemRepository prescriptionItemRepository)
        {
            _patientRepository = patientRepository;
            _addressRepository = addressRepository;
            _diagnosisRepository = diagnosisRepository;
            _prescriptionRepository = prescriptionRepository;
            _prescriptionItemRepository = prescriptionItemRepository;
        }

        public Task<List<PatientDto>> GetAllPatientsAsync()
        {
            return _patientRepository.GetAllPatientsAsync();
        }

        public async Task<bool> AddPatientAsync(PatientCreateDTO dto)
        {
            return await _patientRepository.AddPatientAsync(dto);
        }

        public async Task<bool> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            return await _patientRepository.UpdatePatientAsync(id, dto);
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            return await _patientRepository.DeletePatientAsync(id);
        }

        public async Task<List<PatientDto>> SearchPatientsAsync(string keyword)
        {
            return await _patientRepository.SearchPatientsAsync(keyword);
        }
        public async Task<PatientDetailDto?> GetPatientDetailAsync(int id)
        {
            var patient = await _patientRepository.GetByIdAsync(id);

            if (patient == null)
                return null;

            var addresses = await _addressRepository.GetByUserIdAsync(id);

            var diagnoses = await _diagnosisRepository.GetByPatientIdAsync(id);

            return new PatientDetailDto
            {
                Patient = new PatientDto
                {
                    Id = patient.Id,
                    Username = patient.UserName,
                    Email = patient.Email,
                    PhoneNumber = patient.PhoneNumber,
                    IsActive = patient.IsActive,
                    CreatedAt = patient.CreatedAt
                },

                Addresses = addresses,

                Diagnoses = diagnoses
            };
        }

        public async Task<List<DiagnosisDto>> GetDiagnosisHistoryAsync(int patientId)
        {
            return await _diagnosisRepository.GetDiagnosisHistoryAsync(patientId);
        }

        public async Task<List<PrescriptionDTO>> GetPrescriptionHistoryAsync(int patientId)
        {
            // 1. Lấy danh sách đơn thuốc của bệnh nhân
            var prescriptions = await _prescriptionRepository.GetPrescriptionsByPatientIdAsync(patientId);
            var resultList = new List<PrescriptionDTO>();

            foreach (var p in prescriptions)
            {
                // 2. Mapping sang PrescriptionDTO
                var dto = new PrescriptionDTO
                {
                    PrescriptionId = p.Id,
                    PrescriptionDate = p.PrescriptionDate,
                    DoctorName = p.DoctorName, // Ăn khớp hoàn toàn với thuộc tính Entity mới của bạn
                    Status = p.Status,
                    Items = new List<PrescriptionItemDTO>()
                };

                // 3. Lấy chi tiết các thuốc trong đơn từ PrescriptionItemRepository
                var items = await _prescriptionItemRepository.GetPrescriptionItemsByPrescriptionIdAsync(p.Id);
                foreach (var item in items)
                {
                    dto.Items.Add(new PrescriptionItemDTO
                    {
                        PrescriptionItemId = item.Id,
                        PrescriptionId = item.PrescriptionId,
                        MedicineId = item.MedicineId,
                        MedicineName = item.Medicine?.Name, // Bảo ngọc lấy từ quan hệ Navigation Property
                        Quantity = item.Quantity
                    });
                }

                resultList.Add(dto);
            }

            return resultList;
        }


    }
}
