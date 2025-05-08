using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Collections.Generic;  // Add this line
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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
            var agentRole = await _roleManager.FindByNameAsync("Agent");
            if (agentRole != null)
            {
                var agentUsers = await _userManager.GetUsersInRoleAsync("Agent");
                return View(agentUsers);
            }
            return View(new List<IdentityUser>());
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
                    
                    // Store additional agent information in a separate table or claims
                    // Example of adding claims
                    await _userManager.AddClaimsAsync(user, new Claim[]
                    {
                        new Claim("FirstName", model.FirstName ?? string.Empty),
                        new Claim("LastName", model.LastName ?? string.Empty),
                        new Claim("AddressLine1", model.AddressLine1 ?? string.Empty),
                        new Claim("AddressLine2", model.AddressLine2 ?? string.Empty),
                        new Claim("City", model.City ?? string.Empty),
                        new Claim("State", model.State ?? string.Empty),
                        new Claim("PostalCode", model.PostalCode ?? string.Empty),
                        new Claim("Country", model.Country ?? string.Empty),
                        new Claim("LicenseNumber", model.LicenseNumber ?? string.Empty),
                        new Claim("DateOfBirth", model.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty),
                        new Claim("Bio", model.Bio ?? string.Empty)
                    });

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

    public class CreateAgentViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [StringLength(100)]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [StringLength(100)]
        public string AddressLine2 { get; set; }

        [Display(Name = "City")]
        [StringLength(50)]
        public string City { get; set; }

        [Display(Name = "State/Province")]
        [StringLength(50)]
        public string State { get; set; }

        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string PostalCode { get; set; }

        [Display(Name = "Country")]
        [StringLength(50)]
        public string Country { get; set; }

        [Display(Name = "License Number")]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Agent Bio")]
        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string Bio { get; set; }
    }
}