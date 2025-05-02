using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;

namespace Volunteer_website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if(User.IsInRole("2"))
            {
                return RedirectToAction("Index", "HomeAdmin", new { area = "Admin"});
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Events() {
            return View("Events");
        }
        public IActionResult Fundraising()
        {
            return View();
        } 
        public IActionResult Volunteers()
        {
            return View();
        }
        public IActionResult Blogs() { 
           return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }
    }
}
