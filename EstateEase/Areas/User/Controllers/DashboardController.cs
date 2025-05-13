using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Create view model for dashboard summary
            var viewModel = new UserDashboardViewModel
            {
                // Properties summary
                OwnedPropertiesCount = await _context.UserProperties.CountAsync(p => p.UserId == userId && p.OwnershipType == "Bought"),
                RentedPropertiesCount = await _context.UserProperties.CountAsync(p => p.UserId == userId && p.OwnershipType == "Rented"),
                
                // Recent properties (bought/rented)
                RecentProperties = await _context.UserProperties
                    .Include(p => p.Property)
                    .ThenInclude(p => p.PropertyImages)
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.AcquisitionDate)
                    .Take(3)
                    .Select(p => new PropertySummaryViewModel
                    {
                        Id = p.PropertyId,
                        Title = p.Property.Title,
                        Address = p.Property.Address,
                        Price = p.Property.Price,
                        Type = p.Property.PropertyType,
                        OwnershipType = p.OwnershipType,
                        AcquisitionDate = p.AcquisitionDate,
                        ImageUrl = p.Property.PropertyImages.Any()
                            ? (p.Property.PropertyImages.FirstOrDefault() != null 
                                ? p.Property.PropertyImages.FirstOrDefault().ImagePath 
                                : "/uploads/properties/placeholder.jpg")
                            : "/uploads/properties/placeholder.jpg"
                    })
                    .ToListAsync()
            };
            
            return View(viewModel);
        }
    }
} 