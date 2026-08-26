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

        public async Task<PatientOperationResult> AddPatientAsync(PatientCreateDTO dto)
        {
            return await _patientRepository.AddPatientAsync(dto);
        }

        public async Task<PatientOperationResult> UpdatePatientAsync(int id, UpdatePatientDto dto)
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
                    Username = patient.UserName ?? "",
                    Email = patient.Email ?? "",
                    PhoneNumber = patient.PhoneNumber ?? "",
                    MedicalHistory = patient.MedicalHistory,
                    IsActive = patient.IsActive,
                    CreatedAt = patient.CreatedAt
                },

                Addresses = addresses,

                Diagnoses = diagnoses
            };
        }

        public async Task<List<DiagnosisDTOs>> GetDiagnosisHistoryAsync(int patientId)
        {
            return await _diagnosisRepository.GetDiagnosisHistoryAsync(patientId);
        }

        public async Task<List<PrescriptionResponseDTO>> GetPrescriptionHistoryAsync(int patientId)
        {
            // Lấy danh sách đơn thuốc của bệnh nhân
            var prescriptions = await _prescriptionRepository.GetPrescriptionsByPatientIdAsync(patientId);

            var resultList = new List<PrescriptionResponseDTO>();

            foreach (var p in prescriptions)
            {
                int effectivePatientId = p.PatientId ?? p.UserId;
                var dto = new PrescriptionResponseDTO
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = p.User?.FullName ?? p.User?.UserName ?? "",
                    PatientId = effectivePatientId,
                    PatientName = effectivePatientId == p.UserId
                        ? (p.User?.FullName ?? p.User?.UserName ?? "")
                        : (p.Patient?.FullName ?? p.Patient?.UserName ?? ""),
                    IsSubmittedForOther = p.PatientId.HasValue && p.PatientId.Value != p.UserId,
                    DiagnosisId = p.DiagnosisId ,
                    DoctorId = p.DoctorId,
                    DoctorName = p.DoctorName  ,
                    Hospital = p.Hospital,
                    PrescriptionDate = p.PrescriptionDate,
                    ImageUrl = p.ImageUrl,
                    Status = p.Status,
                    Items = new List<PrescriptionItemResponseDTO>()
                };

                var items = await _prescriptionItemRepository
                    .GetPrescriptionItemsByPrescriptionIdAsync(p.Id);

                foreach (var item in items)
                {
                    dto.Items.Add(new PrescriptionItemResponseDTO
                    {
                        Id = item.Id,
                        MedicineId = item.MedicineId,
                        MedicineName = item.Medicine?.Name ?? "",
                        Quantity = item.Quantity,
                        RequiresPrescription = item.Medicine?.RequiresPrescription ?? false,
                        Instructions = item.Instructions
                    });
                }

                resultList.Add(dto);
            }

            return resultList;
        }


    }
}
