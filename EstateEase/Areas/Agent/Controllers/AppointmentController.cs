using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class AppointmentController : Controller
    {
        public IActionResult Index()
        {
            // This would fetch appointments from database
            return View();
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Save appointment logic here

            TempData["Success"] = "Appointment scheduled successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            // Fetch appointment and return view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update appointment logic here

            TempData["Success"] = "Appointment updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            // Fetch appointment details and return view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            // Cancel appointment logic here

            TempData["Success"] = "Appointment cancelled successfully";
            return RedirectToAction(nameof(Index));
        }
    }
} 