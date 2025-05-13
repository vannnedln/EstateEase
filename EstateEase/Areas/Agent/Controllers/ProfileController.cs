using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public ProfileController(
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

            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (agent == null)
            {
                return NotFound();
            }

            // Get property statistics
            var totalProperties = await _context.Properties
                .Where(p => p.AgentId == agent.Id)
                .CountAsync();

            var soldProperties = await _context.Properties
                .Where(p => p.AgentId == agent.Id && p.Status == "Sold")
                .CountAsync();

            var rentalProperties = await _context.Properties
                .Where(p => p.AgentId == agent.Id && p.SellingType == "Rent")
                .CountAsync();

            var model = new AgentProfileViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = agent.FirstName,
                LastName = agent.LastName,
                AddressLine1 = agent.AddressLine1,
                Barangay = agent.Barangay,
                City = agent.City,
                PostalCode = agent.PostalCode,
                Country = agent.Country,
                LicenseNumber = agent.LicenseNumber,
                Bio = agent.Bio,
                ProfilePictureUrl = agent.ProfilePictureUrl ?? "/images/avatar-01.png",
                TotalProperties = totalProperties,
                SoldProperties = soldProperties,
                RentalProperties = rentalProperties
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AgentProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (agent == null)
            {
                return NotFound();
            }

            // Update user's phone number
            user.PhoneNumber = model.PhoneNumber;
            var userResult = await _userManager.UpdateAsync(user);
            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(model.Password))
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            // Handle profile picture upload
            if (model.ProfilePicture != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "agents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                string uniqueFileName = $"{Guid.NewGuid()}_{model.ProfilePicture.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                // Update the profile picture URL
                agent.ProfilePictureUrl = $"/uploads/agents/{uniqueFileName}";
            }

            // Update agent profile
            agent.FirstName = model.FirstName;
            agent.LastName = model.LastName;
            agent.AddressLine1 = model.AddressLine1;
            agent.Barangay = model.Barangay;
            agent.City = model.City;
            agent.PostalCode = model.PostalCode;
            agent.Country = model.Country;
            agent.LicenseNumber = model.LicenseNumber;
            agent.Bio = model.Bio;

            _context.Agents.Update(agent);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction(nameof(Index));
        }
    }

    public class AgentProfileViewModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AddressLine1 { get; set; }
        public string Barangay { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string LicenseNumber { get; set; }
        public string Bio { get; set; }
        public string ProfilePictureUrl { get; set; }
        public IFormFile ProfilePicture { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        
        // Property statistics
        public int TotalProperties { get; set; }
        public int SoldProperties { get; set; }
        public int RentalProperties { get; set; }
    }
}