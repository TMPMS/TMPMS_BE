using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Models;

namespace TMPMS.Hubs
{
    [Authorize]
    public class PharmacyChatHub : Hub
    {
        private readonly TMPMSDbContext _context;
        private readonly UserManager<User> _userManager;

        public PharmacyChatHub(TMPMSDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? GetUserIdStr()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   Context.User?.FindFirst("sub")?.Value ??
                   Context.User?.FindFirst("id")?.Value ??
                   Context.User?.FindFirst("nameid")?.Value;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = GetUserIdStr();
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _userManager.FindByIdAsync(userIdStr);
                if (user != null)
                {
                    bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(user, "Pharmacy") ||
                                             await _userManager.IsInRoleAsync(user, "Admin");

                    if (isPharmacyOrAdmin)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "pharmacy-dashboard");
                    }
                    // Người dùng thường không còn tự tạo/tìm session ở đây nữa — trước đây vừa có
                    // REST GetMySession vừa có OnConnectedAsync cùng tự tìm-hoặc-tạo session độc lập,
                    // hai request này chạy song song (không đợi nhau) nên có thể race và tạo ra 2
                    // session "Open" khác nhau cho cùng 1 user. Khi đó widget gửi tin theo session id
                    // lấy từ REST, nhưng connection SignalR lại join vào group của session kia (do
                    // OnConnectedAsync tạo ra) -> tin nhắn bị gửi vào group mà chính người gửi không
                    // ở trong đó, nên không thấy tin nhắn của mình dội lại. Giờ client phải gọi
                    // JoinSession(sessionId) tường minh với id lấy từ REST sau khi đã có, đảm bảo luôn
                    // join đúng group của session thật sự đang dùng.
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinSession(int sessionId)
        {
            var userIdStr = GetUserIdStr();
            if (!int.TryParse(userIdStr, out int userId)) return;

            var user = await _userManager.FindByIdAsync(userIdStr);
            if (user == null) return;

            var session = await _context.PharmacyChatSessions.FindAsync(sessionId);
            if (session == null) return;

            bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(user, "Pharmacy") ||
                                     await _userManager.IsInRoleAsync(user, "Admin");
            if (!isPharmacyOrAdmin && session.UserId != userId) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        }

        public async Task SendMessage(int sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var userIdStr = GetUserIdStr();
            if (!int.TryParse(userIdStr, out int userId)) return;

            var user = await _userManager.FindByIdAsync(userIdStr);
            if (user == null) return;

            var session = await _context.PharmacyChatSessions.FindAsync(sessionId);
            if (session == null || session.Status == "Closed") return;

            string senderRole = "User";
            if (await _userManager.IsInRoleAsync(user, "Pharmacy")) senderRole = "Pharmacy";
            else if (await _userManager.IsInRoleAsync(user, "Admin")) senderRole = "Admin";

            var msg = new PharmacyChatMessage
            {
                SessionId = sessionId,
                SenderId = userId,
                SenderRole = senderRole,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.PharmacyChatMessages.Add(msg);
            session.LastMessageAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var messageDto = new
            {
                id = msg.Id,
                sessionId = msg.SessionId,
                senderId = msg.SenderId,
                senderName = user.FullName ?? user.UserName,
                senderRole = msg.SenderRole,
                content = msg.Content,
                sentAt = msg.SentAt,
                isRead = msg.IsRead
            };

            // Broadcast to the chat session group AND pharmacy dashboard group
            await Clients.Group(sessionId.ToString()).SendAsync("ReceiveMessage", messageDto);
            await Clients.Group("pharmacy-dashboard").SendAsync("ReceiveMessage", messageDto);

            // Broadcast notification to pharmacy dashboard group
            await Clients.Group("pharmacy-dashboard").SendAsync("SessionUpdated", new
            {
                sessionId = session.Id,
                userId = session.UserId,
                lastMessage = msg.Content,
                lastMessageAt = session.LastMessageAt,
                senderRole = msg.SenderRole,
                status = session.Status
            });
        }

        public async Task AssignPharmacist(int sessionId)
        {
            var userIdStr = GetUserIdStr();
            if (!int.TryParse(userIdStr, out int userId)) return;

            var pharmacist = await _userManager.FindByIdAsync(userIdStr);
            if (pharmacist == null) return;

            // Explicit validation: target must have role "Pharmacy" or "Admin"
            bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(pharmacist, "Pharmacy") ||
                                     await _userManager.IsInRoleAsync(pharmacist, "Admin");
            if (!isPharmacyOrAdmin)
            {
                throw new HubException("Chỉ tài khoản Dược sĩ (Pharmacy) mới được nhận xử lý hội thoại này.");
            }

            var session = await _context.PharmacyChatSessions.FindAsync(sessionId);
            if (session == null || session.Status == "Closed") return;

            if (session.Status != "Open" && session.AssignedPharmacistId != userId)
            {
                throw new HubException("Phiên tư vấn này đã được tiếp nhận bởi Dược sĩ khác.");
            }

            session.AssignedPharmacistId = userId;
            session.Status = "Assigned";
            await _context.SaveChangesAsync();

            string pharmacistName = pharmacist.FullName ?? pharmacist.UserName ?? "Dược sĩ";

            // Broadcast status change to session group AND pharmacy dashboard group
            await Clients.Group(sessionId.ToString()).SendAsync("PharmacistAssigned", new
            {
                sessionId = session.Id,
                pharmacistId = userId,
                pharmacistName = pharmacistName,
                systemNotice = $"Dược sĩ {pharmacistName} đã tiếp nhận hội thoại và đang hỗ trợ bạn."
            });
            await Clients.Group("pharmacy-dashboard").SendAsync("PharmacistAssigned", new
            {
                sessionId = session.Id,
                pharmacistId = userId,
                pharmacistName = pharmacistName,
                systemNotice = $"Dược sĩ {pharmacistName} đã tiếp nhận hội thoại và đang hỗ trợ bạn."
            });

            // Broadcast to pharmacy dashboard group
            await Clients.Group("pharmacy-dashboard").SendAsync("SessionUpdated", new
            {
                sessionId = session.Id,
                userId = session.UserId,
                assignedPharmacistId = userId,
                assignedPharmacistName = pharmacistName,
                status = session.Status
            });
        }

        public async Task CloseSession(int sessionId)
        {
            var userIdStr = GetUserIdStr();
            if (!int.TryParse(userIdStr, out int userId)) return;

            var user = await _userManager.FindByIdAsync(userIdStr);
            if (user == null) return;

            bool isPharmacyOrAdmin = await _userManager.IsInRoleAsync(user, "Pharmacy") ||
                                     await _userManager.IsInRoleAsync(user, "Admin");
            if (!isPharmacyOrAdmin)
            {
                throw new HubException("Chỉ Dược sĩ hoặc Admin mới có quyền đóng hội thoại.");
            }

            var session = await _context.PharmacyChatSessions.FindAsync(sessionId);
            if (session == null) return;

            session.Status = "Closed";
            await _context.SaveChangesAsync();

            await Clients.Group(sessionId.ToString()).SendAsync("SessionClosed", new
            {
                sessionId = session.Id,
                systemNotice = "Phiên tư vấn đã kết thúc."
            });

            await Clients.Group("pharmacy-dashboard").SendAsync("SessionUpdated", new
            {
                sessionId = session.Id,
                status = "Closed"
            });
        }
    }
}
