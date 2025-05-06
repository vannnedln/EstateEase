using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Collections.Generic;  // Add this line

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

        public async Task<IActionResult> UserList()
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var usersInRole = await userManager.GetUsersInRoleAsync("User");
            return View(usersInRole);
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