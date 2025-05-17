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
                // Repopulate statistics for the view
                var user = await _userManager.GetUserAsync(User);
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == user.Id);
                
                if (agent != null)
                {
                    var totalProperties = await _context.Properties
                        .Where(p => p.AgentId == agent.Id)
                        .CountAsync();

                    var soldProperties = await _context.Properties
                        .Where(p => p.AgentId == agent.Id && p.Status == "Sold")
                        .CountAsync();

                    var rentalProperties = await _context.Properties
                        .Where(p => p.AgentId == agent.Id && p.SellingType == "Rent")
                        .CountAsync();
                        
                    model.TotalProperties = totalProperties;
                    model.SoldProperties = soldProperties;
                    model.RentalProperties = rentalProperties;
                    model.ProfilePictureUrl = agent.ProfilePictureUrl ?? "/images/avatar-01.png";
                }
                
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            var currentAgent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUser.Id);

            if (currentAgent == null)
            {
                return NotFound();
            }

            // Update user's phone number
            currentUser.PhoneNumber = model.PhoneNumber;
            var userResult = await _userManager.UpdateAsync(currentUser);
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

                var token = await _userManager.GeneratePasswordResetTokenAsync(currentUser);
                var passwordResult = await _userManager.ResetPasswordAsync(currentUser, token, model.Password);
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
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                // Delete old profile picture if it exists and is not the default
                if (!string.IsNullOrEmpty(currentAgent.ProfilePictureUrl) && 
                    !currentAgent.ProfilePictureUrl.Contains("avatar-01.png"))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                        currentAgent.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        catch (Exception)
                        {
                            // Log the error but continue
                        }
                    }
                }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "agents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                string uniqueFileName = $"{currentUser.Id}_{Guid.NewGuid()}{Path.GetExtension(model.ProfilePicture.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                // Update the profile picture URL
                currentAgent.ProfilePictureUrl = $"/uploads/agents/{uniqueFileName}";
            }

            // Update agent profile
            currentAgent.FirstName = model.FirstName;
            currentAgent.LastName = model.LastName;
            currentAgent.AddressLine1 = model.AddressLine1;
            currentAgent.Barangay = model.Barangay;
            currentAgent.City = model.City;
            currentAgent.PostalCode = model.PostalCode;
            currentAgent.Country = model.Country;
            currentAgent.LicenseNumber = model.LicenseNumber;
            currentAgent.Bio = model.Bio;

            _context.Agents.Update(currentAgent);
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