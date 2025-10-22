using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> index()
    {
        //fetching users from external database
        var users = await _context.Users.ToListAsync();
        var stemmer = await _context.Stemmers.ToListAsync();

        if (users.Any())
        {
            Console.WriteLine($"Found {users.Count} users");
            foreach (var user in users) 
            {
                Console.WriteLine($"User Found: {user.Firstname} {user.Lastname} {user.Fodselsnr} {user.Passord}");
            }
        }

        /*Kode for å skive ut alle kommuner
         if (stemmer.Any())
        {
            Console.WriteLine($"found kommune");
            foreach (var stemme in stemmer)
            {
                Console.WriteLine($"Parti found: {stemme.Kommune}");
            }
        }*/
        
        else
        {
            Console.WriteLine("User Not Found");
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
}