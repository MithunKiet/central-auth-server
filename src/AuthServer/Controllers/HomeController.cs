using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

public class HomeController : Controller
{
    [HttpGet("")]
    [HttpGet("home")]
    public IActionResult Index() => View();

    [HttpGet("error")]
    public IActionResult Error(string? message)
    {
        ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
        return View("~/Views/Shared/Error.cshtml");
    }
}
