using Microsoft.AspNetCore.Mvc;
using EstateEase.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using EstateEase.Models.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TestController(ApplicationDbContext context, UserManager<IdentityUser> userManager = null)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Test that we can access all the DbSets
            var properties = await _context.Properties.CountAsync();
            var agents = await _context.Agents.CountAsync();
            var userProperties = await _context.UserProperties.CountAsync();

            ViewBag.Properties = properties;
            ViewBag.Agents = agents;
            ViewBag.UserProperties = userProperties;

            return View();
        }
        
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateTestProperty()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not logged in");
            }
            
            // Debug info
            TempData["DebugUserId"] = userId;
            TempData["DebugUserIdOriginalCase"] = userId; // Store exact case for debugging
            
            // Check if agent exists, if not create one
            var agent = await _context.Agents.FirstOrDefaultAsync();
            if (agent == null)
            {
                // Get user info for the agent
                var user = await _userManager.FindByNameAsync("admin@estateease.com");
                if (user == null)
                {
                    // Create a new user for the agent if admin doesn't exist
                    user = new IdentityUser
                    {
                        UserName = "testagent@estateease.com",
                        Email = "testagent@estateease.com",
                        EmailConfirmed = true
                    };
                    await _userManager.CreateAsync(user, "Test@123");
                    await _userManager.AddToRoleAsync(user, "Agent");
                }
                
                agent = new Agent
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    FirstName = "Test",
                    LastName = "Agent",
                    AddressLine1 = "123 Test Street",
                    Barangay = "Test Barangay",
                    City = "Test City",
                    PostalCode = "12345",
                    Country = "Philippines",
                    LicenseNumber = "TEST-123456",
                    Bio = "This is a test agent created for testing purposes.",
                    ProfilePictureUrl = "/images/avatar-01.png"
                };
                
                _context.Agents.Add(agent);
                await _context.SaveChangesAsync();
            }
            
            // Create a test property
            var property = new Property
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Test Property",
                Content = "This is a test property created for testing purposes.",
                PropertyType = "Apartment",
                Bedrooms = 2,
                Bathrooms = 1,
                Balcony = 1,
                Kitchen = 1,
                Hall = 1,
                Price = 1000000,
                Size = 100,
                Address = "123 Test Street, Test City",
                Status = "Available",
                TotalFloors = 5,
                IsFeatured = false,
                SellingType = "Sale",
                HasSwimmingPool = false,
                HasParking = true,
                HasGym = false,
                HasSecurity = true,
                HasElevator = true,
                HasCCTV = false,
                AgentId = agent.Id,
                CreatedAt = DateTime.Now
            };
            
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();
            
            // Create a property image
            var propertyImage = new PropertyImage
            {
                Id = Guid.NewGuid().ToString(),
                PropertyId = property.Id,
                ImagePath = "/uploads/properties/placeholder.jpg",
                ImageType = "Property",
                CreatedAt = DateTime.Now
            };
            _context.PropertyImages.Add(propertyImage);
            await _context.SaveChangesAsync();
            
            // Associate the property with the user (as "Bought")
            var userProperty = new UserProperty
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                PropertyId = property.Id,
                OwnershipType = "Bought",
                RelationshipType = "Owner",
                AcquisitionDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };
            _context.UserProperties.Add(userProperty);
            await _context.SaveChangesAsync();
            
            // Store the property ID for debugging
            TempData["DebugBoughtPropertyId"] = property.Id;
            
            // Create another test property for rental
            var rentalProperty = new Property
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Test Rental Property",
                Content = "This is a test rental property created for testing purposes.",
                PropertyType = "Apartment",
                Bedrooms = 1,
                Bathrooms = 1,
                Balcony = 1,
                Kitchen = 1,
                Hall = 1,
                Price = 15000,
                Size = 75,
                Address = "456 Test Avenue, Test City",
                Status = "Available",
                TotalFloors = 10,
                IsFeatured = false,
                SellingType = "Rent",
                HasSwimmingPool = true,
                HasParking = true,
                HasGym = true,
                HasSecurity = true,
                HasElevator = true,
                HasCCTV = true,
                AgentId = agent.Id,
                CreatedAt = DateTime.Now
            };
            
            _context.Properties.Add(rentalProperty);
            await _context.SaveChangesAsync();
            
            // Create a property image for the rental
            var rentalImage = new PropertyImage
            {
                Id = Guid.NewGuid().ToString(),
                PropertyId = rentalProperty.Id,
                ImagePath = "/uploads/properties/placeholder.jpg",
                ImageType = "Property",
                CreatedAt = DateTime.Now
            };
            _context.PropertyImages.Add(rentalImage);
            await _context.SaveChangesAsync();
            
            // Associate the rental property with the user
            var rentalUserProperty = new UserProperty
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId, // Keep the exact userId from claims
                PropertyId = rentalProperty.Id,
                OwnershipType = "Rented",
                RelationshipType = "Renter",
                AcquisitionDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddMonths(12),
                CreatedAt = DateTime.Now
            };
            _context.UserProperties.Add(rentalUserProperty);
            await _context.SaveChangesAsync();
            
            // Store the rental property ID for debugging
            TempData["DebugRentedPropertyId"] = rentalProperty.Id;
            
            // Check if the properties were successfully added
            // Use case-insensitive comparison to count properties
            var userPropertiesCount = await _context.UserProperties
                .Where(up => EF.Functions.Collate(up.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId)
                .CountAsync();
            
            TempData["DebugUserPropertiesCount"] = userPropertiesCount;
            TempData["Success"] = $"Successfully created {userPropertiesCount} test properties for user {userId}";
            
            return RedirectToAction("Index", "Dashboard", new { area = "User" });
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> CheckUserProperties()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not logged in");
            }
            
            // Get all user properties using case-insensitive comparison
            var userProperties = await _context.UserProperties
                .Include(p => p.Property)
                .Where(p => EF.Functions.Collate(p.UserId, "SQL_Latin1_General_CP1_CI_AS") == userId)
                .ToListAsync();
                
            ViewBag.UserProperties = userProperties;
            ViewBag.OwnedCount = userProperties.Count(p => p.OwnershipType == "Bought");
            ViewBag.RentedCount = userProperties.Count(p => p.OwnershipType == "Rented");
            ViewBag.MissingOwnershipCount = userProperties.Count(p => string.IsNullOrEmpty(p.OwnershipType));
            ViewBag.UserId = userId;
            
            return View();
        }
        
        // New method to check all user properties in the database
        public async Task<IActionResult> CheckAllUserProperties()
        {
            var userProperties = await _context.UserProperties
                .Include(p => p.Property)
                .Include(p => p.User)
                .ToListAsync();
                
            ViewBag.UserProperties = userProperties;
            ViewBag.TotalCount = userProperties.Count;
            ViewBag.OwnedCount = userProperties.Count(p => p.OwnershipType == "Bought");
            ViewBag.RentedCount = userProperties.Count(p => p.OwnershipType == "Rented");
            ViewBag.MissingOwnershipCount = userProperties.Count(p => string.IsNullOrEmpty(p.OwnershipType));
            
            return View("CheckUserProperties");
        }
    }
} 