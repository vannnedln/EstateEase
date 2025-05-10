using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> AdminList()
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                return View(adminUsers);
            }
            return View(new List<IdentityUser>());
        }

        public async Task<IActionResult> AgentList()
        {
            try
            {
                var agents = await _context.Agents
                    .Include(a => a.User)
                    .Include(a => a.Properties)
                    .Select(a => new AgentListViewModel
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        Email = a.User.Email,
                        PhoneNumber = a.User.PhoneNumber,
                        ProfilePictureUrl = a.ProfilePictureUrl ?? "/images/avatar-01.png",
                        LicenseNumber = a.LicenseNumber,
                        PropertyCount = a.Properties.Count,
                        DateOfBirth = a.DateOfBirth
                    })
                    .ToListAsync();

                return View(agents);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AgentList action: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the agent list.";
                return View(new List<AgentListViewModel>());
            }
        }

        public async Task<IActionResult> UserList()
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var usersInRole = await userManager.GetUsersInRoleAsync("User");
            return View(usersInRole);
        }

        // GET: Admin/User/CreateAgent
        public IActionResult CreateAgent()
        {
            return View();
        }

        // POST: Admin/User/CreateAgent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgent(CreateAgentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    // Add the user to the Agent role
                    await _userManager.AddToRoleAsync(user, "Agent");
                    
                    // Handle profile picture upload
                    string profilePictureUrl = "/images/avatar-01.png"; // Default image
                    if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                    {
                        // Create unique filename
                        string uniqueFileName = $"{Guid.NewGuid()}_{model.ProfilePicture.FileName}";
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/agents");
                        
                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProfilePicture.CopyToAsync(fileStream);
                        }
                        
                        profilePictureUrl = $"/uploads/agents/{uniqueFileName}";
                    }
                    
                    // Create and save the agent profile
                    var agent = new Models.Entities.Agent
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        AddressLine1 = model.AddressLine1,
                        Barangay = model.Barangay,
                        City = model.City,
                        PostalCode = model.PostalCode,
                        Country = model.Country,
                        LicenseNumber = model.LicenseNumber,
                        DateOfBirth = model.DateOfBirth,
                        Bio = model.Bio,
                        ProfilePictureUrl = profilePictureUrl
                    };

                    _context.Agents.Add(agent);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Agent account created successfully!";
                    return RedirectToAction(nameof(AgentList));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User deleted successfully" });
            }

            return Json(new { success = false, message = "Failed to delete user" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            IdentityResult result;

            if (isAdmin)
            {
                result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            else
            {
                result = await _userManager.AddToRoleAsync(user, "Admin");
            }

            if (result.Succeeded)
            {
                return Json(new
                {
                    success = true,
                    message = isAdmin ? "Admin role removed successfully" : "Admin role added successfully",
                    isAdmin = !isAdmin
                });
            }

            return Json(new { success = false, message = "Failed to update user role" });
        }
    }
}