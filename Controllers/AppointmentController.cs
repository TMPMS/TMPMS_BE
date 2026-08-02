using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookAppointment(AppointmentCreateDTO dto)
        {
            try
            {
                int userId = 0;
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null) int.TryParse(claim.Value, out userId);

                bool result = await _appointmentService.BookAppointment(userId, dto);
                return Ok(new { success = result, message = "Book appointment successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("my-appointments")]
        [Authorize]
        public async Task<IActionResult> GetAppointments()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var appointments = await _appointmentService.GetAppointments(userId);
            return Ok(appointments);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointments();
            return Ok(appointments);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAppointment(int id, AppointmentUpdateDTO dto)
        {
            try
            {
                bool result = await _appointmentService.UpdateAppointment(id, dto);
                if (!result) return BadRequest(new { success = false, message = "Update appointment failed." });
                return Ok(new { success = true, message = "Appointment updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.DeleteAppointment(id);
                if (!result) return NotFound(new { success = false, message = "Appointment not found." });
                return Ok(new { success = true, message = "Appointment deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("cancel/{id}")]
        [Authorize]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.CancelAppointment(id);
                return Ok(new { Success = result, Message = "Appointment cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("approve/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.ApproveAppointment(id);
                return Ok(new { Success = result, Message = "Appointment approved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("complete/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.CompleteAppointment(id);
                return Ok(new { Success = result, Message = "Appointment completed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
    }
}
