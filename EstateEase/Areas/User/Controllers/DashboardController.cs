using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            ILogger<DashboardController> logger = null)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Verify user authentication
            var isAuthenticated = User.Identity.IsAuthenticated;
            var userName = User.Identity.Name;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var isInUserRole = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "User");
            
            ViewBag.Debug_IsAuthenticated = isAuthenticated;
            ViewBag.Debug_UserName = userName;
            ViewBag.Debug_UserRoles = userRoles;
            ViewBag.Debug_IsInUserRole = isInUserRole;
            
            // Fix any UserProperty records with missing OwnershipType values
            await FixUserPropertyRecords(userId);
            
            // Debug: Count user properties before creating view model
            var ownedCount = await _context.UserProperties
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId && p.OwnershipType == "Bought")
                .CountAsync();
                
            var rentedCount = await _context.UserProperties
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId && p.OwnershipType == "Rented")
                .CountAsync();
                
            var totalCount = ownedCount + rentedCount;
            
            // Add debug info to ViewBag
            ViewBag.Debug_UserId = userId;
            ViewBag.Debug_OwnedCount = ownedCount;
            ViewBag.Debug_RentedCount = rentedCount;
            ViewBag.Debug_TotalCount = totalCount;
            
            // Log the query for debugging
            var userProperties = await _context.UserProperties
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId)
                .ToListAsync();
                
            ViewBag.Debug_UserPropertiesRaw = userProperties;
            
            // Create view model for dashboard summary
            var viewModel = new UserDashboardViewModel
            {
                // Properties summary
                OwnedPropertiesCount = ownedCount,
                RentedPropertiesCount = rentedCount,
                
                // Recent properties (bought/rented)
                RecentProperties = await _context.UserProperties
                    .Include(p => p.Property)
                    .ThenInclude(p => p.PropertyImages)
                    .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId)
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

        // Helper method to fix UserProperty records with missing OwnershipType values
        private async Task FixUserPropertyRecords(string userId)
        {
            var userProperties = await _context.UserProperties
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId && (p.OwnershipType == null || p.OwnershipType == ""))
                .ToListAsync();

            foreach (var property in userProperties)
            {
                // Set OwnershipType based on RelationshipType
                if (property.RelationshipType == "Owner")
                {
                    property.OwnershipType = "Bought";
                }
                else if (property.RelationshipType == "Renter")
                {
                    property.OwnershipType = "Rented";
                }
                else
                {
                    // Default to "Bought" if RelationshipType is also missing
                    property.OwnershipType = "Bought";
                }

                // Set AcquisitionDate if missing
                if (property.AcquisitionDate == default)
                {
                    property.AcquisitionDate = property.CreatedAt;
                }

                // Set ExpiryDate for rentals if missing
                if (property.OwnershipType == "Rented" && property.ExpiryDate == null)
                {
                    property.ExpiryDate = property.CreatedAt.AddMonths(12);
                }
            }

            if (userProperties.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
} 