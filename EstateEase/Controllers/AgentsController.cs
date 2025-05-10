using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstateEase.Controllers
{
    public class AgentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AgentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var agents = await _context.Agents.ToListAsync();
            return View(agents);
        }
    }
} 