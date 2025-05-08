using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            // This would fetch payment history from database
            return View();
        }

        public IActionResult Details(int id)
        {
            // Fetch payment details and return view
            return View();
        }

        public IActionResult Download(int id)
        {
            // Generate invoice PDF and return as file download
            return File("path/to/invoice.pdf", "application/pdf", $"invoice_{id}.pdf");
        }
    }
} 