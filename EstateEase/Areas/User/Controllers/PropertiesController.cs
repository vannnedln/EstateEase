using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PropertiesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string ownershipFilter = "All")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Fix any UserProperty records with missing OwnershipType values
            await FixUserPropertyRecords(userId);

            var query = _context.UserProperties
                .Include(p => p.Property)
                .ThenInclude(p => p.PropertyImages)
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId);

            // Apply filter if needed
            if (ownershipFilter != "All")
            {
                query = query.Where(p => p.OwnershipType == ownershipFilter);
            }

            var properties = await query
                .OrderByDescending(p => p.AcquisitionDate)
                .Select(p => new UserPropertyViewModel
                {
                    Id = p.Id,
                    PropertyId = p.PropertyId,
                    Title = p.Property.Title,
                    Address = p.Property.Address,
                    Price = p.Property.Price,
                    Type = p.Property.PropertyType,
                    Bedrooms = p.Property.Bedrooms,
                    Bathrooms = p.Property.Bathrooms,
                    Size = p.Property.Size,
                    OwnershipType = p.OwnershipType,
                    AcquisitionDate = p.AcquisitionDate,
                    ExpiryDate = p.ExpiryDate,
                    Status = p.Property.Status,
                    ImageUrl = p.Property.PropertyImages.Any()
                        ? (p.Property.PropertyImages.FirstOrDefault() != null 
                            ? p.Property.PropertyImages.FirstOrDefault().ImagePath 
                            : "/uploads/properties/placeholder.jpg")
                        : "/uploads/properties/placeholder.jpg"
                })
                .ToListAsync();

            var viewModel = new UserPropertiesViewModel
            {
                Properties = properties,
                OwnershipFilter = ownershipFilter,
                TotalCount = properties.Count(),
                OwnedCount = properties.Count(p => p.OwnershipType == "Bought"),
                RentedCount = properties.Count(p => p.OwnershipType == "Rented")
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userProperty = await _context.UserProperties
                .Include(p => p.Property)
                .ThenInclude(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == id && EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId);

            if (userProperty == null)
            {
                return NotFound();
            }

            var viewModel = new UserPropertyDetailViewModel
            {
                Id = userProperty.Id,
                PropertyId = userProperty.PropertyId,
                Title = userProperty.Property.Title,
                Content = userProperty.Property.Content,
                Address = userProperty.Property.Address,
                Price = userProperty.Property.Price,
                Type = userProperty.Property.PropertyType,
                Bedrooms = userProperty.Property.Bedrooms,
                Bathrooms = userProperty.Property.Bathrooms,
                Kitchen = userProperty.Property.Kitchen,
                Balcony = userProperty.Property.Balcony,
                Hall = userProperty.Property.Hall,
                TotalFloors = userProperty.Property.TotalFloors,
                Size = userProperty.Property.Size,
                OwnershipType = userProperty.OwnershipType,
                AcquisitionDate = userProperty.AcquisitionDate,
                ExpiryDate = userProperty.ExpiryDate,
                Status = userProperty.Property.Status,
                HasSwimmingPool = userProperty.Property.HasSwimmingPool,
                HasParking = userProperty.Property.HasParking,
                HasGym = userProperty.Property.HasGym,
                HasSecurity = userProperty.Property.HasSecurity,
                HasElevator = userProperty.Property.HasElevator,
                HasCCTV = userProperty.Property.HasCCTV,
                Images = userProperty.Property.PropertyImages.Select(i => new UserPropertyImageViewModel
                {
                    Id = i.Id,
                    ImagePath = i.ImagePath,
                    ImageType = i.ImageType
                }).ToList()
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