using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> BookAppointment(AppointmentCreateDTO dto)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                bool result = await _appointmentService.BookAppointment(userId, dto);

                return Ok(new
                {
                    success = result,
                    message = "Book appointment successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("my-appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var appointments = await _appointmentService.GetAppointments(userId);

            return Ok(appointments);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(
    int id,
    AppointmentUpdateDTO dto)
        {
            try
            {
                bool result = await _appointmentService.UpdateAppointment(id, dto);

                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Update appointment failed."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Appointment updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Patient")]
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.CancelAppointment(id);

                return Ok(new
                {
                    Success = result,
                    Message = "Appointment cancelled successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("approve/{id}")]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.ApproveAppointment(id);

                return Ok(new
                {
                    Success = result,
                    Message = "Appointment approved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("complete/{id}")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.CompleteAppointment(id);

                return Ok(new
                {
                    Success = result,
                    Message = "Appointment completed successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

    }
}
