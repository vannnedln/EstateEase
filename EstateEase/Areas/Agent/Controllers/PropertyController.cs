using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class PropertyController : Controller
    {
        public IActionResult Index()
        {
            // This would fetch properties from database
            return View();
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(PropertyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Save property logic here

            TempData["Success"] = "Property added successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            // Fetch property and return view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, PropertyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update property logic here

            TempData["Success"] = "Property updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            // Fetch property details and return view
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            // Delete property logic here

            TempData["Success"] = "Property deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }

    public class PropertyViewModel
    {
        // Property fields would go here
    }
} 