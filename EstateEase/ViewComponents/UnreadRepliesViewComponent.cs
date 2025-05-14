using Microsoft.AspNetCore.Mvc;
using EstateEase.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.ViewComponents
{
    public class UnreadRepliesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UnreadRepliesViewComponent(
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
            var isUser = await _userManager.IsInRoleAsync(user, "User");
            
            int unreadCount = 0;
            
            if (isUser)
            {
                // Get unread inquiry replies for user
                unreadCount = await _context.Inquiries
                    .Where(i => i.UserId == userId && i.ReadByUser == false)
                    .CountAsync();
            }
            
            return View(unreadCount);
        }
    }
} 