using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EstateEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProperties = await _context.Properties
                .Where(p => p.IsFeatured)
                .Include(p => p.PropertyImages)
                .Include(p => p.Agent)
                .Take(6) // Limit to 6 featured properties
                .ToListAsync();
                
            return View(featuredProperties);
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        
        public async Task<IActionResult> Properties()
        {
            var properties = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Agent)
                .ToListAsync();
                
            return View(properties);
        }
        
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
