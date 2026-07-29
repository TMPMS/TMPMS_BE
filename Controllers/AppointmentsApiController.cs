using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("appointments")]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public AppointmentsApiController(TMPMSDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Select(a => new {
                    id = a.Id,
                    patientId = a.UserId,
                    patientName = a.User.FullName ?? a.User.UserName,
                    patientPhone = a.User.PhoneNumber,
                    doctorId = a.StaffId ?? 10,
                    doctorName = a.Staff != null ? (a.Staff.FullName ?? a.Staff.UserName) : "Bác sĩ phụ trách",
                    appointmentDate = a.AppointmentDate,
                    reason = a.Reason,
                    status = a.Status == "Pending" ? "Scheduled" : a.Status, // align with React expected status
                    notes = a.Note,
                    created_at = a.CreatedAt
                })
                .ToListAsync();

            return Ok(appointments);
        }

        public class AppointmentInput
        {
            public int PatientId { get; set; }
            public int DoctorId { get; set; }
            public DateTime AppointmentDate { get; set; }
            public string Reason { get; set; } = "";
            public string Status { get; set; } = "Scheduled";
            public string Notes { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentInput input)
        {
            // If patientId is 0 or user doesn't exist, we can use the first user
            var userId = input.PatientId;
            if (userId <= 0)
            {
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                userId = firstUser?.Id ?? 1;
            }

            var appt = new Appointment
            {
                UserId = userId,
                StaffId = input.DoctorId > 0 ? input.DoctorId : 10, // Default Doctor account
                AppointmentDate = input.AppointmentDate,
                Reason = input.Reason,
                Status = input.Status == "Scheduled" ? "Pending" : input.Status,
                Note = input.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();

            // Load navigation properties for returned JSON
            await _context.Entry(appt).Reference(a => a.User).LoadAsync();

            return StatusCode(201, new {
                id = appt.Id,
                patientId = appt.UserId,
                patientName = appt.User?.FullName ?? appt.User?.UserName ?? "Bệnh nhân",
                patientPhone = appt.User?.PhoneNumber,
                doctorId = appt.StaffId,
                doctorName = "Bác sĩ phụ trách",
                appointmentDate = appt.AppointmentDate,
                reason = appt.Reason,
                status = appt.Status == "Pending" ? "Scheduled" : appt.Status,
                notes = appt.Note,
                created_at = appt.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentInput input)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound("Appointment not found");

            if (input.PatientId > 0) appt.UserId = input.PatientId;
            if (input.DoctorId > 0) appt.StaffId = input.DoctorId;
            if (input.AppointmentDate != default) appt.AppointmentDate = input.AppointmentDate;
            if (!string.IsNullOrEmpty(input.Reason)) appt.Reason = input.Reason;
            appt.Status = input.Status == "Scheduled" ? "Pending" : input.Status;
            appt.Note = input.Notes;

            await _context.SaveChangesAsync();
            return Ok(appt);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound("Appointment not found");

            _context.Appointments.Remove(appt);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }
    }
}
