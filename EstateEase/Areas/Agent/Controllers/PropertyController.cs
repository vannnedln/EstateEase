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


            TempData["Success"] = "Property added successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
    
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

      

            TempData["Success"] = "Property updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
      

            TempData["Success"] = "Property deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }

    public class PropertyViewModel
    {
      
    }
} 