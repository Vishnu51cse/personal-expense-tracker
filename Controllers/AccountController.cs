using Microsoft.AspNetCore.Mvc;

namespace PersonalExpenseTracker.Controllers
{
    /// <summary>
    /// Serves Razor Views for User Registration and Login.
    /// </summary>
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}
