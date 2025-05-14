using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System;
using EstateEase.Models.ViewModels;
using System.Collections.Generic;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get agent ID for the current user
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null)
                {
                    TempData["Error"] = "Agent profile not found.";
                    return View();
                }
                
                // Get active properties count
                var activePropertiesCount = await _context.Properties
                    .Where(p => p.AgentId == agent.Id && p.Status == "Available")
                    .CountAsync();
                
                // Get new inquiries count - using placeholder since table doesn't exist
                var newInquiriesCount = 0; // Placeholder value
                
                // Calculate total income - 3% of the price of sold properties
                var soldProperties = await _context.Properties
                    .Where(p => p.AgentId == agent.Id && (p.Status == "Sold" || p.Status == "Rented"))
                    .ToListAsync();
                
                var totalIncome = soldProperties.Sum(p => p.Price * 0.03m); // Calculate 3% commission
                
                // Get recent properties
                var recentProperties = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .Where(p => p.AgentId == agent.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new PropertyViewModel
                    {
                        Id = p.Id,
                        Title = p.Title,
                        PropertyType = p.PropertyType,
                        Price = p.Price,
                        Status = p.Status,
                        Address = p.Address,
                        ExistingImages = p.PropertyImages.Select(pi => new PropertyImageViewModel
                        {
                            Id = pi.Id,
                            ImagePath = pi.ImagePath,
                            ImageType = pi.ImageType
                        }).ToList()
                    })
                    .ToListAsync();
                
                // Create sample activities since we don't have real data
                var recentActivities = new List<DashboardActivityViewModel>();
                
                // Add recent property activities with actual creation dates
                if (recentProperties.Any())
                {
                    for (int i = 0; i < Math.Min(recentProperties.Count, 3); i++)
                    {
                        var property = recentProperties[i];
                        // Get the actual property from the database to get its CreatedAt date
                        var propertyEntity = await _context.Properties
                            .FirstOrDefaultAsync(p => p.Id == property.Id);
                            
                        recentActivities.Add(new DashboardActivityViewModel
                        {
                            Type = "Property",
                            Message = $"Property listed: {property.Title}",
                            Date = propertyEntity?.CreatedAt ?? DateTime.Now,
                            IconClass = "bi-house-door text-primary",
                            UserId = userId
                        });
                    }
                }
                
                // Add recent inquiries if they exist
                var recentInquiries = await _context.Inquiries
                    .Where(i => i.AgentId == agent.Id)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(3)
                    .ToListAsync();
                    
                foreach (var inquiry in recentInquiries)
                {
                    recentActivities.Add(new DashboardActivityViewModel
                    {
                        Type = "Inquiry",
                        Message = $"New inquiry: {inquiry.Subject}",
                        Date = inquiry.CreatedAt,
                        IconClass = "bi-chat-dots text-info",
                        UserId = userId
                    });
                }
                
                // Sort by date
                recentActivities = recentActivities
                    .OrderByDescending(a => a.Date)
                    .Take(5)
                    .ToList();
                
                // Pass data to view
                ViewBag.ActivePropertiesCount = activePropertiesCount;
                ViewBag.NewInquiriesCount = newInquiriesCount;
                ViewBag.TotalIncome = totalIncome;
                ViewBag.RecentProperties = recentProperties;
                ViewBag.RecentActivities = recentActivities;
                ViewBag.AgentName = agent.FirstName;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading dashboard data: " + ex.Message;
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
    
    public class DashboardActivityViewModel
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public string IconClass { get; set; }
        public string UserId { get; set; }
    }
}