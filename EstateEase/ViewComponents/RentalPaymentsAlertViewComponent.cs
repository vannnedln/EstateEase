using EstateEase.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EstateEase.ViewComponents
{
    public class RentalPaymentsAlertViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public RentalPaymentsAlertViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Return early if user not authenticated
            if (!User.Identity.IsAuthenticated)
                return View(0);

            var userId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Get rented properties that need attention (expiring within 30 days)
            var expiringRentals = await _context.UserProperties
                .Where(up => up.UserId == userId && 
                           up.OwnershipType == "Rented" && 
                           up.ExpiryDate.HasValue && 
                           EF.Functions.DateDiffDay(DateTime.Now, up.ExpiryDate.Value) <= 30)
                .CountAsync();
                
            return View(expiringRentals);
        }
    }
} 