using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // ----------------------------
    // Trigger BankID login
    // ----------------------------
    public IActionResult Login()
    {
        // Redirects to Criipto / BankID login
        var redirectUri = Url.Action("Index", "Home"); // redirect after login
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri },
                         OpenIdConnectDefaults.AuthenticationScheme);
    }

    // ----------------------------
    // Protected page example
    // ----------------------------
    [Authorize]
    public IActionResult Protected()
    {
        return View();
    }

    // ----------------------------
    // Logout
    // ----------------------------
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index");
    }

    // ----------------------------
    // Main page
    // ----------------------------
    public async Task<IActionResult> Index()
    {
        // ----------------------------
        // Read info from OIDC claims
        // ----------------------------
        string? fullName = null;
        string? fodselsnr = null;
        string? email = null;
        string? phone = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            fullName = User.FindFirst("name")?.Value;
            fodselsnr = User.FindFirst("sub")?.Value;      // BankID fødselsnummer
            email = User.FindFirst("email")?.Value;       // BankID email
            phone = User.FindFirst("phone_number")?.Value; // BankID telefon (om tilgjengelig)

            HttpContext.Session.SetString("UserFullName", fullName ?? "");
            HttpContext.Session.SetString("Fodselsnr", fodselsnr ?? "");
        }

        // ----------------------------
        // Fetch extra info from DB if user exists
        // ----------------------------
        User? targetUser = null;
        if (!string.IsNullOrEmpty(fodselsnr))
        {
            targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Fodselsnr == fodselsnr);
        }

        // ----------------------------
        // Combine data into ViewModel
        // ----------------------------
        var model = new HomeViewModel
        {
            FullName = fullName,
            Fodselsnr = fodselsnr,
            Email = email,
            Phone = phone,
            Kommune = targetUser?.Kommune,
            // du kan legge til flere felt her fra DB hvis du ønsker
        };

        if (!string.IsNullOrEmpty(model.Kommune))
        {
            HttpContext.Session.SetString("Kommune", model.Kommune);
        }

        return View(model);
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
}
