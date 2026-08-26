using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{

    [ApiController]
    [Route("api/patients")]
    [Route("patients")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // GET: api/patients
        [HttpGet]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetPatients()
        {
            var patients = await _patientService.GetAllPatientsAsync();

            return Ok(patients);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> CreatePatient([FromBody] PatientCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _patientService.AddPatientAsync(dto);

            if (result.IsConflict)
            {
                return StatusCode(409, new { message = result.Message, conflictUserId = result.ConflictUserId, conflictUserName = result.ConflictUserName });
            }

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                message = result.Message
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdatePatient(int id, UpdatePatientDto dto)
        {
            var result = await _patientService.UpdatePatientAsync(id, dto);

            if (result.IsConflict)
            {
                return StatusCode(409, new { message = result.Message, conflictUserId = result.ConflictUserId, conflictUserName = result.ConflictUserName });
            }

            if (!result.Success)
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            return Ok(new
            {
                message = result.Message
            });
        }

        // DELETE: api/patients/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var result = await _patientService.DeletePatientAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Patient not found."
                });
            }

            return Ok(new
            {
                message = "Delete patient successfully."
            });
        }

        // GET: api/patients/search?keyword=abc
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SearchPatients(string keyword)
        {
            var result = await _patientService.SearchPatientsAsync(keyword);

            return Ok(result);
        }

        // GET: api/patients/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPatientDetail(int id)
        {
            var patient = await _patientService.GetPatientDetailAsync(id);

            if (patient == null)
                return NotFound(new
                {
                    message = "Patient not found."
                });

            return Ok(patient);
        }

        // GET: api/patients/5/diagnosis-history
        [HttpGet("{patientId}/diagnosis-history")]
        [Authorize(Roles = "Admin,Staff,User")]
        public async Task<IActionResult> GetDiagnosisHistory(int patientId)
        {
            // Nếu là User (bệnh nhân tự đăng nhập), chỉ cho phép xem lịch sử chẩn đoán của chính mình.
            var userRoleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRoleClaim == "User")
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int loggedInUserId) || loggedInUserId != patientId)
                {
                    return Forbid();
                }
            }

            var result = await _patientService.GetDiagnosisHistoryAsync(patientId);

            return Ok(result);
        }

        // GET: api/patients/{patientId}/prescription-history
        [HttpGet("{patientId}/prescription-history")]
        [Authorize(Roles = "Admin,Staff,User")]
        public async Task<IActionResult> GetPrescriptionHistory(int patientId)
        {
            // 1. Kiểm tra bảo mật: Nếu là User (bệnh nhân), chỉ cho phép xem đơn thuốc của chính mình
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRoleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRoleClaim == "User")
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int loggedInUserId) || loggedInUserId != patientId)
                {
                    return Forbid(); // Trả về 403 Forbidden nếu cố tình xem đơn thuốc của bệnh nhân khác
                }
            }

            // 2. Gọi Service để lấy lịch sử đơn thuốc (Luồng: Controller -> Service -> Repository)
            var result = await _patientService.GetPrescriptionHistoryAsync(patientId);

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "No prescription history found for this patient."
                });
            }

            return Ok(result);
        }


    }
}
