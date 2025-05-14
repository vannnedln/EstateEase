using EstateEase.Data;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get total properties count
            var propertiesCount = await _context.Properties.CountAsync();
            ViewBag.TotalProperties = propertiesCount;

            // Get registered regular users count (excluding agents and admins)
            // First get all users
            var allUsers = await _userManager.Users.ToListAsync();
            
            // Get admin role users
            var adminRoleId = await _roleManager.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();
            
            // Get agent role users
            var agentRoleId = await _roleManager.Roles
                .Where(r => r.Name == "Agent")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();
            
            // Get users in admin or agent roles
            var adminAndAgentUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRoleId || ur.RoleId == agentRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();
            
            // Count regular users (excluding admins and agents)
            var regularUsersCount = allUsers.Count(u => !adminAndAgentUserIds.Contains(u.Id));
            ViewBag.RegisteredUsers = regularUsersCount;

            // Get registered agents count
            var agentsCount = await _context.Agents.CountAsync();
            ViewBag.RegisteredAgents = agentsCount;

            // Get total revenue (sum of all transactions)
            var totalRevenue = await _context.Transactions
                .Where(t => t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
            
            // Format the revenue to show commas for thousands
            ViewBag.TotalRevenue = totalRevenue.ToString("N0");

            // Get recent properties (limited to 5)
            var recentProperties = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Agent)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentProperties = recentProperties;

            // Get recent activity
            var recentActivities = new List<dynamic>();
            
            // Add recent user profiles instead of users (which have creation dates)
            var recentUserProfiles = await _context.UserProfiles
                .OrderByDescending(u => u.CreatedAt)
                .Take(2)
                .Select(u => new
                {
                    Type = "UserRegistration",
                    Name = u.FirstName + " " + u.LastName,
                    Date = u.CreatedAt,
                    Id = u.UserId
                })
                .ToListAsync();
            
            // Add recent properties
            var recentPropertyActivities = await _context.Properties
                .OrderByDescending(p => p.CreatedAt)
                .Take(2)
                .Select(p => new
                {
                    Type = "PropertyListing",
                    Name = p.Title,
                    Date = p.CreatedAt,
                    Id = p.Id
                })
                .ToListAsync();
            
            // Add recent transactions
            var recentTransactions = await _context.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(2)
                .Select(t => new
                {
                    Type = "Transaction",
                    Name = t.PropertyId,
                    Date = t.CreatedAt,
                    Id = t.Id,
                    Amount = t.Amount
                })
                .ToListAsync();
            
            // Combine all activities and sort by date
            foreach (var user in recentUserProfiles) recentActivities.Add(user);
            foreach (var property in recentPropertyActivities) recentActivities.Add(property);
            foreach (var transaction in recentTransactions) recentActivities.Add(transaction);
            
            ViewBag.RecentActivities = recentActivities
                .OrderByDescending(a => ((DateTime)a.Date))
                .Take(4)
                .ToList();

            // Property type distribution for chart
            var propertyTypes = await _context.Properties
                .GroupBy(p => p.PropertyType)
                .Select(g => new {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            
            ViewBag.PropertyTypeLabels = string.Join("', '", propertyTypes.Select(p => p.Type));
            ViewBag.PropertyTypeCounts = string.Join(", ", propertyTypes.Select(p => p.Count));

            return View();
        }
    }
}