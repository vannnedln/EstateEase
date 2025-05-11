using System;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.Entities;
using EstateEase.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstateEase.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public PropertyController(
            ApplicationDbContext context,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        // GET: Property/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Agent)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var viewModel = new PropertyDetailsViewModel
            {
                Property = property,
                IsUserLoggedIn = _signInManager.IsSignedIn(User)
            };

            return View(viewModel);
        }
    }
} 