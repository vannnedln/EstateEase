using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using EstateEase.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class SettingsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public SettingsController(
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Join data from both AspNetUsers and UserProfiles tables
            var userWithProfile = await (from u in _context.Users
                                        where u.Id == user.Id
                                        join p in _context.UserProfiles on u.Id equals p.UserId into profileJoin
                                        from profile in profileJoin.DefaultIfEmpty()
                                        select new { User = u, Profile = profile }).FirstOrDefaultAsync();

            var model = new UserSettingsViewModel
            {
                Id = userWithProfile.User.Id,
                Email = userWithProfile.User.Email,
                PhoneNumber = userWithProfile.User.PhoneNumber
            };

            if (userWithProfile.Profile != null)
            {
                // Populate all fields from UserProfile entity
                model.FirstName = userWithProfile.Profile.FirstName;
                model.LastName = userWithProfile.Profile.LastName;
                model.Birthday = userWithProfile.Profile.Birthday;
                model.Address = userWithProfile.Profile.Address;
                model.Barangay = userWithProfile.Profile.Barangay;
                model.City = userWithProfile.Profile.City;
                model.PostalCode = userWithProfile.Profile.PostalCode;
                model.Country = userWithProfile.Profile.Country;
                model.CurrentProfilePictureUrl = userWithProfile.Profile.ProfilePictureUrl ?? "/images/avatar-01.png";
            }
            else
            {
                // Default values for users without a profile
                model.FirstName = "";
                model.LastName = "";
                model.Address = "";
                model.Barangay = "";
                model.City = "";
                model.PostalCode = "";
                model.Country = "Philippines"; // Default country
                model.CurrentProfilePictureUrl = "/images/avatar-01.png";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserSettingsViewModel model)
        {
            try
            {
                // Disable model validation temporarily to allow nullable fields
                // but keep required validation
                var keysToRemove = ModelState.Keys
                    .Where(k => k != "Id" && k != "Email")
                    .ToList();
                    
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Update AspNetUsers fields
                user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;
                
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Get or create UserProfile
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                bool isNewProfile = false;
                
                if (userProfile == null)
                {
                    userProfile = new UserProfile
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    isNewProfile = true;
                }

                // Update profile picture if provided as part of the form submission
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    try
                    {
                        // Create unique filename
                        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ProfilePicture.FileName)}";
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "users");
                        
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
                    catch (Exception ex)
                    {
                        // Log the error but continue with the rest of the profile update
                        // This way, even if image upload fails, other profile data is still updated
                        ModelState.AddModelError("ProfilePicture", $"Profile picture upload failed: {ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(model.CurrentProfilePictureUrl))
                {
                    // Keep existing profile picture if available
                    userProfile.ProfilePictureUrl = model.CurrentProfilePictureUrl;
                }

                // Update UserProfile properties
                userProfile.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? "(No name)" : model.FirstName;
                userProfile.LastName = string.IsNullOrWhiteSpace(model.LastName) ? string.Empty : model.LastName;
                userProfile.Birthday = model.Birthday; // Already nullable
                userProfile.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address;
                userProfile.Barangay = string.IsNullOrWhiteSpace(model.Barangay) ? null : model.Barangay;
                userProfile.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City;
                userProfile.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode;
                userProfile.Country = string.IsNullOrWhiteSpace(model.Country) ? "Philippines" : model.Country;
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

                TempData["Success"] = "Profile settings updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                // Get or create UserProfile
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                bool isNewProfile = false;
                
                if (userProfile == null)
                {
                    userProfile = new UserProfile
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        FirstName = "(No name)",
                        LastName = "",
                        CreatedAt = DateTime.Now,
                        Country = "Philippines"
                    };
                    isNewProfile = true;
                }

                // Get file extension and validate
                var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Invalid file format. Only JPG, PNG and GIF images are allowed." });
                }

                // Create unique filename
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "users");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }
                
                // Update profile picture URL
                userProfile.ProfilePictureUrl = $"/uploads/users/{uniqueFileName}";
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

                return Json(new { 
                    success = true, 
                    message = "Profile picture updated successfully",
                    pictureUrl = userProfile.ProfilePictureUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    public class UserSettingsViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Barangay { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public DateTime? Birthday { get; set; }
        public string CurrentProfilePictureUrl { get; set; }
        
        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePicture { get; set; }
    }
} 