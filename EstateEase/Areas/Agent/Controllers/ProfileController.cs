using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var claims = await _userManager.GetClaimsAsync(user);
            
            var model = new AgentProfileViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = claims.FirstOrDefault(c => c.Type == "FirstName")?.Value,
                LastName = claims.FirstOrDefault(c => c.Type == "LastName")?.Value,
                AddressLine1 = claims.FirstOrDefault(c => c.Type == "AddressLine1")?.Value,
                AddressLine2 = claims.FirstOrDefault(c => c.Type == "AddressLine2")?.Value,
                City = claims.FirstOrDefault(c => c.Type == "City")?.Value,
                State = claims.FirstOrDefault(c => c.Type == "State")?.Value,
                PostalCode = claims.FirstOrDefault(c => c.Type == "PostalCode")?.Value,
                Country = claims.FirstOrDefault(c => c.Type == "Country")?.Value,
                LicenseNumber = claims.FirstOrDefault(c => c.Type == "LicenseNumber")?.Value,
                Bio = claims.FirstOrDefault(c => c.Type == "Bio")?.Value
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

            user.PhoneNumber = model.PhoneNumber;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Get current claims
            var claims = await _userManager.GetClaimsAsync(user);
            
            // Update all profile claims
            await UpdateClaim(user, claims, "FirstName", model.FirstName);
            await UpdateClaim(user, claims, "LastName", model.LastName);
            await UpdateClaim(user, claims, "AddressLine1", model.AddressLine1);
            await UpdateClaim(user, claims, "AddressLine2", model.AddressLine2);
            await UpdateClaim(user, claims, "City", model.City);
            await UpdateClaim(user, claims, "State", model.State);
            await UpdateClaim(user, claims, "PostalCode", model.PostalCode);
            await UpdateClaim(user, claims, "Country", model.Country);
            await UpdateClaim(user, claims, "LicenseNumber", model.LicenseNumber);
            await UpdateClaim(user, claims, "Bio", model.Bio);

            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction(nameof(Index));
        }

        private async Task UpdateClaim(IdentityUser user, IList<Claim> currentClaims, string claimType, string claimValue)
        {
            var existingClaim = currentClaims.FirstOrDefault(c => c.Type == claimType);
            
            if (existingClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, existingClaim);
            }
            
            await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue ?? string.Empty));
        }
    }

    public class AgentProfileViewModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string LicenseNumber { get; set; }
        public string Bio { get; set; }
    }
} 