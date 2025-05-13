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
            var userId = _userManager.GetUserId(User);
            
            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                return NotFound();
            }

            // Get the user profile from UserProfiles table
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Create the view model with data from both tables
            var viewModel = new UserSettingsViewModel
            {
                Id = userId,
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber
            };

            // If user profile exists, populate the view model with profile data
            if (userProfile != null)
            {
                viewModel.FirstName = userProfile.FirstName;
                viewModel.LastName = userProfile.LastName;
                viewModel.Address = userProfile.Address;
                viewModel.Barangay = userProfile.Barangay;
                viewModel.City = userProfile.City;
                viewModel.PostalCode = userProfile.PostalCode;
                viewModel.Country = userProfile.Country;
                viewModel.Birthday = userProfile.Birthday;
                viewModel.CurrentProfilePictureUrl = userProfile.ProfilePictureUrl;
                viewModel.Bio = userProfile.Bio;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Update phone number in AspNetUsers
            if (user.PhoneNumber != model.PhoneNumber)
            {
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
            }

            // Get or create user profile
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            bool isNewProfile = false;

            if (userProfile == null)
            {
                userProfile = new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                isNewProfile = true;
            }

            // Update user profile
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;
            userProfile.Address = model.Address;
            userProfile.Barangay = model.Barangay;
            userProfile.City = model.City;
            userProfile.PostalCode = model.PostalCode;
            userProfile.Country = model.Country;
            userProfile.Birthday = model.Birthday;
            userProfile.ProfilePictureUrl = model.CurrentProfilePictureUrl;
            userProfile.Bio = model.Bio;
            userProfile.UpdatedAt = DateTime.UtcNow;
            
            if (isNewProfile)
            {
                _context.UserProfiles.Add(userProfile);
            }
            else
            {
                _context.UserProfiles.Update(userProfile);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Invalid file type. Allowed types: .jpg, .jpeg, .png, .gif" });
            }

            // Validate file size (max 5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size exceeds the limit of 5MB" });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                
                // Create directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Delete old profile picture if exists
                if (userProfile != null && !string.IsNullOrEmpty(userProfile.ProfilePictureUrl))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new profile picture
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Update user profile
                var pictureUrl = $"/uploads/profiles/{fileName}";
                
                if (userProfile == null)
                {
                    userProfile = new UserProfile
                    {
                        UserId = userId,
                        ProfilePictureUrl = pictureUrl,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserProfiles.Add(userProfile);
                }
                else
                {
                    userProfile.ProfilePictureUrl = pictureUrl;
                    userProfile.UpdatedAt = DateTime.UtcNow;
                    _context.UserProfiles.Update(userProfile);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, pictureUrl = pictureUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error uploading profile picture: {ex.Message}" });
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
        public string Bio { get; set; }
    }
} 