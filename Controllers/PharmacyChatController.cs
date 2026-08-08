using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Models;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    [Authorize]
    public class PharmacyChatController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly UserManager<User> _userManager;

        public PharmacyChatController(TMPMSDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            User.FindFirstValue("sub") ?? 
                            User.FindFirstValue("id") ?? 
                            User.FindFirst("nameid")?.Value;
            if (int.TryParse(userIdStr, out int userId)) return userId;
            return 0;
        }

        // GET: /api/PharmacyChat/my-session
        [HttpGet("my-session")]
        public async Task<IActionResult> GetMySession()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized();

            var session = await _context.PharmacyChatSessions
                .Include(s => s.AssignedPharmacist)
                .Include(s => s.Messages)
                .ThenInclude(m => m.Sender)
                .Where(s => s.UserId == userId && s.Status != "Closed")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                session = new PharmacyChatSession
                {
                    UserId = userId,
                    Status = "Open",
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };
                _context.PharmacyChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            var result = new
            {
                id = session.Id,
                userId = session.UserId,
                assignedPharmacistId = session.AssignedPharmacistId,
                assignedPharmacistName = session.AssignedPharmacist?.FullName ?? session.AssignedPharmacist?.UserName,
                status = session.Status,
                createdAt = session.CreatedAt,
                lastMessageAt = session.LastMessageAt,
                messages = session.Messages.OrderBy(m => m.SentAt).Select(m => new
                {
                    id = m.Id,
                    sessionId = m.SessionId,
                    senderId = m.SenderId,
                    senderName = m.Sender?.FullName ?? m.Sender?.UserName,
                    senderRole = m.SenderRole,
                    content = m.Content,
                    sentAt = m.SentAt,
                    isRead = m.IsRead
                })
            };

            return Ok(result);
        }

        // GET: /api/PharmacyChat/sessions
        [HttpGet("sessions")]
        [Authorize(Roles = "Admin,Pharmacy")]
        public async Task<IActionResult> GetSessions([FromQuery] string? status)
        {
            var query = _context.PharmacyChatSessions
                .Include(s => s.User)
                .Include(s => s.AssignedPharmacist)
                .Include(s => s.Messages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var sessions = await query
                .OrderByDescending(s => s.LastMessageAt)
                .Select(s => new
                {
                    id = s.Id,
                    userId = s.UserId,
                    userName = s.User != null ? (s.User.FullName ?? s.User.UserName) : "Khách hàng",
                    userPhone = s.User != null ? s.User.PhoneNumber : null,
                    userEmail = s.User != null ? s.User.Email : null,
                    assignedPharmacistId = s.AssignedPharmacistId,
                    assignedPharmacistName = s.AssignedPharmacist != null ? (s.AssignedPharmacist.FullName ?? s.AssignedPharmacist.UserName) : null,
                    status = s.Status,
                    createdAt = s.CreatedAt,
                    lastMessageAt = s.LastMessageAt,
                    lastMessage = s.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
                    unreadCount = s.Messages.Count(m => !m.IsRead && m.SenderRole == "User")
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // GET: /api/PharmacyChat/sessions/{id}/messages
        [HttpGet("sessions/{id}/messages")]
        public async Task<IActionResult> GetSessionMessages(int id)
        {
            int currentUserId = GetCurrentUserId();
            var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
            if (currentUser == null) return Unauthorized();

            var session = await _context.PharmacyChatSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Không tìm thấy phiên hội thoại" });

            bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(currentUser, "Pharmacy") ||
                                     await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isPharmacyOrAdmin && session.UserId != currentUserId)
            {
                return Forbid();
            }

            var messages = await _context.PharmacyChatMessages
                .Include(m => m.Sender)
                .Where(m => m.SessionId == id)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id,
                    sessionId = m.SessionId,
                    senderId = m.SenderId,
                    senderName = m.Sender != null ? (m.Sender.FullName ?? m.Sender.UserName) : "Nguồn khác",
                    senderRole = m.SenderRole,
                    content = m.Content,
                    sentAt = m.SentAt,
                    isRead = m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: /api/PharmacyChat/sessions/{id}/assign
        [HttpPost("sessions/{id}/assign")]
        [Authorize(Roles = "Admin,Pharmacy")]
        public async Task<IActionResult> AssignSession(int id)
        {
            int pharmacistId = GetCurrentUserId();
            var pharmacist = await _userManager.FindByIdAsync(pharmacistId.ToString());
            if (pharmacist == null) return Unauthorized();

            // Explicit role validation check
            bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(pharmacist, "Pharmacy") ||
                                     await _userManager.IsInRoleAsync(pharmacist, "Admin");
            if (!isPharmacyOrAdmin)
            {
                return BadRequest(new { message = "Chỉ tài khoản Dược sĩ (Pharmacy) hoặc Admin mới được nhận gán phiên tư vấn." });
            }

            var session = await _context.PharmacyChatSessions.FindAsync(id);
            if (session == null) return NotFound(new { message = "Không tìm thấy phiên hội thoại" });

            // Race-condition protection: prevent double assign if already taken by another pharmacist
            if (session.Status != "Open" && session.AssignedPharmacistId != pharmacistId)
            {
                return StatusCode(409, new { message = "Phiên tư vấn này đã được tiếp nhận bởi Dược sĩ khác." });
            }

            session.AssignedPharmacistId = pharmacistId;
            session.Status = "Assigned";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gán Dược sĩ phụ trách thành công.", sessionId = session.Id, assignedPharmacistId = pharmacistId });
        }
    }
}
