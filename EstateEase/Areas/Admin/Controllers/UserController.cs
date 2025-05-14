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
            try
            {
                // Get all users in the "User" role
                var usersInRole = await _userManager.GetUsersInRoleAsync("User");
                var userIds = usersInRole.Select(u => u.Id).ToList();
                
                // Get all users with optional profiles
                var userList = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .GroupJoin(
                        _context.UserProfiles,
                        user => user.Id,
                        profile => profile.UserId,
                        (user, profiles) => new { User = user, Profiles = profiles })
                    .SelectMany(
                        x => x.Profiles.DefaultIfEmpty(),
                        (x, profile) => new UserListViewModel
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            Email = x.User.Email,
                            FirstName = profile != null ? profile.FirstName : "(No name)",
                            LastName = profile != null ? profile.LastName : "",
                            ProfilePictureUrl = profile != null && !string.IsNullOrEmpty(profile.ProfilePictureUrl) 
                                ? profile.ProfilePictureUrl 
                                : "/images/avatar-01.png",
                            CreatedAt = profile != null ? profile.CreatedAt : DateTime.Now
                        })
                    .ToListAsync();

                return View(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UserList action: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the user list.";
                return View(new List<UserListViewModel>());
            }
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

        // GET: Admin/User/Edit/5
        [HttpGet]
        [Route("Admin/User/Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            _logger.LogInformation($"Edit action called with id: {id}");

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Edit action received null or empty id");
                TempData["Error"] = "User ID was not provided.";
                return RedirectToAction(nameof(UserList));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"No user found with id: {id}");
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserList));
                }

                _logger.LogInformation($"Found user with email: {user.Email}");

                var model = new UserEditViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                // First check if this is an agent
                var agent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.UserId == id);

                if (agent != null)
                {
                    _logger.LogInformation($"Found agent profile for user: {user.Email}");
                    model.FirstName = agent.FirstName;
                    model.LastName = agent.LastName;
                    model.Birthday = agent.DateOfBirth;
                    model.Address = agent.AddressLine1;
                    model.Barangay = agent.Barangay;
                    model.City = agent.City;
                    model.PostalCode = agent.PostalCode;
                    model.Country = agent.Country;
                    model.CurrentProfilePictureUrl = agent.ProfilePictureUrl ?? "/images/avatar-01.png";
                    return View(model);
                }

                // If not an agent, check for a user profile
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == id);

                if (userProfile != null)
                {
                    _logger.LogInformation($"Found user profile for user: {user.Email}");
                    model.FirstName = userProfile.FirstName;
                    model.LastName = userProfile.LastName;
                    model.Birthday = userProfile.Birthday;
                    model.Address = userProfile.Address;
                    model.Barangay = userProfile.Barangay;
                    model.City = userProfile.City;
                    model.PostalCode = userProfile.PostalCode;
                    model.Country = userProfile.Country;
                    model.CurrentProfilePictureUrl = userProfile.ProfilePictureUrl ?? "/images/avatar-01.png";
                }
                else
                {
                    _logger.LogInformation($"No profile found for user: {user.Email}, using defaults");
                    model.FirstName = "(No name)";
                    model.LastName = "";
                    model.CurrentProfilePictureUrl = "/images/avatar-01.png";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Edit action: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the user profile.";
                return RedirectToAction(nameof(UserList));
            }
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [Route("Admin/User/Edit/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            try
            {
                _logger.LogInformation($"Edit POST action called for user ID: {model.Id}");
                
                if (model == null || string.IsNullOrEmpty(model.Id))
                {
                    _logger.LogWarning("Edit POST received null model or model with empty ID");
                    TempData["Error"] = "Invalid form submission. User ID is missing.";
                    return RedirectToAction(nameof(UserList));
                }

                // Skip validation to allow nullable fields
                ModelState.Clear();

                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    _logger.LogWarning($"No user found with id: {model.Id}");
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserList));
                }

                _logger.LogInformation($"Found user: {user.Email}, updating basic properties");

                // Always update IdentityUser properties
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    user.Email = model.Email;
                    user.UserName = model.Email; // Also set username to match email
                }
                
                user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;
                
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning($"Error updating user: {error.Description}");
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Check if user is in Agent role
                bool isAgent = await _userManager.IsInRoleAsync(user, "Agent");
                _logger.LogInformation($"User {user.Email} is agent: {isAgent}");

                if (isAgent)
                {
                    // Update agent profile
                    var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == model.Id);
                    
                    if (agent != null)
                    {
                        _logger.LogInformation($"Updating agent profile for {user.Email}");
                        agent.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? "(No name)" : model.FirstName;
                        agent.LastName = string.IsNullOrWhiteSpace(model.LastName) ? string.Empty : model.LastName;
                        agent.DateOfBirth = model.Birthday;
                        agent.AddressLine1 = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address;
                        agent.Barangay = string.IsNullOrWhiteSpace(model.Barangay) ? null : model.Barangay;
                        agent.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City;
                        agent.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode;
                        agent.Country = string.IsNullOrWhiteSpace(model.Country) ? null : model.Country;
                        
                        // Update profile picture if provided
                        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                        {
                            _logger.LogInformation("Processing profile picture upload for agent");
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
                            
                            agent.ProfilePictureUrl = $"/uploads/agents/{uniqueFileName}";
                        }
                        
                        _context.Agents.Update(agent);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Agent profile updated successfully");
                        TempData["Success"] = "Agent profile updated successfully";
                        return RedirectToAction(nameof(AgentList));
                    }
                    else
                    {
                        _logger.LogWarning($"User {user.Email} is in Agent role but has no agent profile");
                        TempData["Error"] = "User is an agent but has no agent profile.";
                        return RedirectToAction(nameof(AgentList));
                    }
                }
                else
                {
                    // Handle regular user profile
                    _logger.LogInformation($"Updating regular user profile for {user.Email}");
                    var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == model.Id);
                    bool isNewProfile = false;
                    
                    if (userProfile == null)
                    {
                        _logger.LogInformation("Creating new user profile");
                        userProfile = new UserProfile
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = model.Id,
                            CreatedAt = DateTime.Now
                        };
                        isNewProfile = true;
                    }

                    // Update profile picture if provided
                    if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                    {
                        _logger.LogInformation("Processing profile picture upload for user");
                        // Create unique filename
                        string uniqueFileName = $"{Guid.NewGuid()}_{model.ProfilePicture.FileName}";
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/users");
                        
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
                        
                        userProfile.ProfilePictureUrl = $"/uploads/users/{uniqueFileName}";
                    }

                    // Update UserProfile properties
                    userProfile.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? "(No name)" : model.FirstName;
                    userProfile.LastName = string.IsNullOrWhiteSpace(model.LastName) ? string.Empty : model.LastName;
                    userProfile.Birthday = model.Birthday;
                    userProfile.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address;
                    userProfile.Barangay = string.IsNullOrWhiteSpace(model.Barangay) ? null : model.Barangay;
                    userProfile.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City;
                    userProfile.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode;
                    userProfile.Country = string.IsNullOrWhiteSpace(model.Country) ? null : model.Country;
                    userProfile.UpdatedAt = DateTime.Now;

                    // Add or update the profile
                    if (isNewProfile)
                    {
                        _context.UserProfiles.Add(userProfile);
                    }
                    else
                    {
                        _context.UserProfiles.Update(userProfile);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User profile updated successfully");
                    TempData["Success"] = "User updated successfully";
                    return RedirectToAction(nameof(UserList));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Edit POST action: {ex.Message}");
                TempData["Error"] = "An error occurred while saving changes: " + ex.Message;
                return View(model);
            }
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

        // GET: Admin/User/View/5
        [HttpGet]
        [Route("Admin/User/View/{id}")]
        public new async Task<IActionResult> View(string id)
        {
            _logger.LogInformation($"View action called with id: {id}");
            
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("View action received null or empty id");
                TempData["Error"] = "User ID was not provided.";
                return RedirectToAction(nameof(UserList));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"No user found with id: {id}");
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserList));
                }

                _logger.LogInformation($"Found user with email: {user.Email}");
                
                var model = new UserEditViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                // First check if this is an agent
                var agent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.UserId == id);

                if (agent != null)
                {
                    _logger.LogInformation($"Found agent profile for user: {user.Email}");
                    model.FirstName = agent.FirstName;
                    model.LastName = agent.LastName;
                    model.Birthday = agent.DateOfBirth;
                    model.Address = agent.AddressLine1;
                    model.Barangay = agent.Barangay;
                    model.City = agent.City;
                    model.PostalCode = agent.PostalCode;
                    model.Country = agent.Country;
                    model.CurrentProfilePictureUrl = agent.ProfilePictureUrl ?? "/images/avatar-01.png";
                    return View(model);
                }

                // If not an agent, check for a user profile
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == id);

                if (userProfile != null)
                {
                    _logger.LogInformation($"Found user profile for user: {user.Email}");
                    model.FirstName = userProfile.FirstName;
                    model.LastName = userProfile.LastName;
                    model.Birthday = userProfile.Birthday;
                    model.Address = userProfile.Address;
                    model.Barangay = userProfile.Barangay;
                    model.City = userProfile.City;
                    model.PostalCode = userProfile.PostalCode;
                    model.Country = userProfile.Country;
                    model.CurrentProfilePictureUrl = userProfile.ProfilePictureUrl ?? "/images/avatar-01.png";
                }
                else
                {
                    _logger.LogInformation($"No profile found for user: {user.Email}, using defaults");
                    // Default values for users without a profile
                    model.FirstName = "(No name)";
                    model.LastName = "";
                    model.CurrentProfilePictureUrl = "/images/avatar-01.png";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in View action: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the user profile.";
                return RedirectToAction(nameof(UserList));
            }
        }
    }
}