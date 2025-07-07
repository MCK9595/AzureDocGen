using Microsoft.AspNetCore.Mvc;

namespace AzureDocGen.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}