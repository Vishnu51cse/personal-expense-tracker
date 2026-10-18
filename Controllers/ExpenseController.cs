using Microsoft.AspNetCore.Mvc;

namespace PersonalExpenseTracker.Controllers
{
    /// <summary>
    /// Serves Razor Views for the Expense Tracker Dashboard, Add Expense, and Edit Expense screens.
    /// Data is fetched and manipulated via client-side calls to the JWT-protected Expenses API.
    /// </summary>
    public class ExpenseController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.ExpenseId = id;
            return View();
        }
    }
}
