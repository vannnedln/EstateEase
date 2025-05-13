using Microsoft.AspNetCore.Mvc;
using EstateEase.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EstateEase.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Test that we can access all the DbSets
            var properties = await _context.Properties.CountAsync();
            var agents = await _context.Agents.CountAsync();
            var userProperties = await _context.UserProperties.CountAsync();

            ViewBag.Properties = properties;
            ViewBag.Agents = agents;
            ViewBag.UserProperties = userProperties;

            return View();
        }
    }
} 