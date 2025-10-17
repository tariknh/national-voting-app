using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string fodselsnr, string password)
    {
        Console.WriteLine("Somthing somthing");
        // Find user in database by Fodselsnr
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Fodselsnr == fodselsnr && u.Passord == password);
        
        if (user != null)
        {
            HttpContext.Session.SetString("UserFullName", $"{user.Firstname} {user.Lastname}");
            HttpContext.Session.SetString("Fodselsnr", user.Fodselsnr);
            
            // Redirect to home page or dashboard
            return RedirectToAction("Index", "Home");
        }
        else
        {
            // Login failed
            ViewBag.ErrorMessage = "Invalid personnummer or password";
            return View();
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}