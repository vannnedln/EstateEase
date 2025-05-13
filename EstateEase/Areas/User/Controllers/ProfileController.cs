using EstateEase.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // Create the view model with data from both tables
            var viewModel = new UserProfileViewModel
            {
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber
            };

            // If user profile exists, populate the view model with profile data
            if (userProfile != null)
            {
                viewModel.FirstName = userProfile.FirstName;
                viewModel.LastName = userProfile.LastName;
                viewModel.AddressLine1 = userProfile.Address;
                viewModel.AddressLine2 = userProfile.Barangay;
                viewModel.City = userProfile.City;
                viewModel.PostalCode = userProfile.PostalCode;
                viewModel.Country = userProfile.Country;
                viewModel.Bio = userProfile.Bio;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfileViewModel model)
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
                    UserId = userId,
                    CreatedAt = System.DateTime.UtcNow
                };
                isNewProfile = true;
            }

            // Update profile data
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;
            userProfile.Address = model.AddressLine1;
            userProfile.Barangay = model.AddressLine2;
            userProfile.City = model.City;
            userProfile.PostalCode = model.PostalCode;
            userProfile.Country = model.Country;
            userProfile.Bio = model.Bio;
            userProfile.UpdatedAt = System.DateTime.UtcNow;

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
        public string Bio { get; set; }
    }
} 