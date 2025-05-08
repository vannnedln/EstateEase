using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class InquiryController : Controller
    {
        public IActionResult Index()
        {
            // This would fetch inquiries from database
            return View();
        }

        public IActionResult Details(int id)
        {
            // Fetch inquiry details and return view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reply(int id, InquiryReplyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Send reply logic here

            TempData["Success"] = "Reply sent successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsResolved(int id)
        {
            // Mark inquiry as resolved logic here

            TempData["Success"] = "Inquiry marked as resolved";
            return RedirectToAction(nameof(Index));
        }
    }
} 