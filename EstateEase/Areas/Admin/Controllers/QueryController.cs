using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class QueryController : Controller
    {
        public IActionResult ManagePayments()
        {
            return View();
        }

        public IActionResult ViewPayments()
        {
            return View();
        }
    }
} 