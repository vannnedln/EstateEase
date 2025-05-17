using EstateEase.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            
            // Get user from AspNetUsers
            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                return NotFound();
            }

            // Get the user profile from UserProfiles table
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Create a new profile if one doesn't exist
            bool isNewProfile = false;
            if (userProfile == null)
            {
                userProfile = new EstateEase.Models.Entities.UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    FirstName = GetNameFromEmail(identityUser.Email),
                    LastName = "",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();
                isNewProfile = true;
            }

            // Get user statistics
            var inquiriesCount = await _context.Inquiries
                .Where(i => i.UserId == userId)
                .CountAsync();

            var messagesCount = await _context.InquiryMessages
                .Where(m => m.SenderId == userId)
                .CountAsync();

            // Create the view model with data from both tables
            var viewModel = new UserProfileViewModel
            {
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber,
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                AddressLine1 = userProfile.Address,
                AddressLine2 = userProfile.Barangay,
                City = userProfile.City,
                PostalCode = userProfile.PostalCode,
                Country = userProfile.Country ?? "Philippines",
                Bio = userProfile.Bio ?? "",
                ProfilePictureUrl = userProfile.ProfilePictureUrl ?? "/images/avatar-01.png",
                InquiriesCount = inquiriesCount,
                MessagesCount = messagesCount
            };

            if (isNewProfile)
            {
                TempData["Success"] = "Welcome! We've created a profile for you. Please complete your information.";
            }

            return View(viewModel);
        }

        private string GetNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "User";
                
            // Extract name part from email (before the @ symbol)
            var namePart = email.Split('@')[0];
            
            // Capitalize first letter and convert the rest to lowercase
            if (!string.IsNullOrEmpty(namePart) && namePart.Length > 0)
            {
                return char.ToUpper(namePart[0]) + (namePart.Length > 1 ? namePart.Substring(1).ToLower() : "");
            }
            
            return "User";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfileViewModel model, IFormFile ProfilePicture)
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
                userProfile = new Models.Entities.UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    FirstName = model.FirstName ?? GetNameFromEmail(user.Email),
                    LastName = model.LastName ?? "",
                    CreatedAt = DateTime.UtcNow
                };
                isNewProfile = true;
            }

                // Update profile data
            userProfile.FirstName = model.FirstName ?? userProfile.FirstName;
            userProfile.LastName = model.LastName ?? userProfile.LastName;
            userProfile.Address = model.AddressLine1 ?? userProfile.Address;
            userProfile.Barangay = model.AddressLine2 ?? userProfile.Barangay;
            userProfile.City = model.City ?? userProfile.City;
            userProfile.PostalCode = model.PostalCode ?? userProfile.PostalCode;
            userProfile.Country = model.Country ?? userProfile.Country ?? "Philippines";
                userProfile.Bio = model.Bio ?? userProfile.Bio ?? "";
            userProfile.UpdatedAt = DateTime.UtcNow;
            
            // Handle profile picture upload
            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                // Delete old profile picture if it exists
                if (!string.IsNullOrEmpty(userProfile.ProfilePictureUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new profile picture
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = $"{userId}_{Guid.NewGuid().ToString()}{Path.GetExtension(ProfilePicture.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicture.CopyToAsync(fileStream);
                }

                userProfile.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
            }

            if (isNewProfile)
            {
                _context.UserProfiles.Add(userProfile);
            }
            else
            {
                _context.UserProfiles.Update(userProfile);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction(nameof(Index));
        }
    }

    public class UserProfileViewModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Bio { get; set; } = "";
        public string ProfilePictureUrl { get; set; }
        public IFormFile ProfilePicture { get; set; }
        public int InquiriesCount { get; set; }
        public int MessagesCount { get; set; }
    }
} 