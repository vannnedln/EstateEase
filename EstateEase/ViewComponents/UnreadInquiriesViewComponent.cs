using Microsoft.AspNetCore.Mvc;
using EstateEase.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.ViewComponents
{
    public class UnreadInquiriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UnreadInquiriesViewComponent(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return View(0);
            }

            var userId = user.Id;
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
            
            int unreadCount = 0;
            
            if (isAdmin)
            {
                // Get unread inquiries for admin
                unreadCount = await _context.Inquiries
                    .Include(i => i.Property)
                    .Where(i => (i.AgentId == userId || i.AgentId == null || 
                              i.Property.AgentId == userId || i.Property.AgentId == null) && 
                              i.Status == "New")
                    .CountAsync();
            }
            else if (isAgent)
            {
                // Get agent ID
                var agent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.UserId == userId);
                    
                var agentId = agent?.Id;
                
                // Get unread inquiries for agent
                unreadCount = await _context.Inquiries
                    .Where(i => (i.AgentId == agentId || i.AgentId == userId) && i.Status == "New")
                    .CountAsync();
            }
            
            return View(unreadCount);
        }
    }
} 