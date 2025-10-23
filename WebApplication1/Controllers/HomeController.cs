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
    
    public async Task<IActionResult> Index()
    {
        var fullName = HttpContext.Session.GetString("UserFullName");
        var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
        
        User? targetUser = null;
        if (!string.IsNullOrEmpty(fodselsnr))
        {
            targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Fodselsnr == fodselsnr);
        }
        
        
        //this logs all users just to get the list of them in console
        //----
        var users =  await _context.Users.ToListAsync();

        if (users.Any())
        {
            Console.WriteLine($"Found {users.Count} users");
            foreach (var user in users) 
            {
                Console.WriteLine($"User Found: {user.Firstname} {user.Lastname} {user.Fodselsnr} {user.Passord}");
            }
        }
        //----

        var model = new HomeViewModel
        {
            FullName = fullName,
            Fodselsnr = fodselsnr,
            Kommune = targetUser?.Kommune,
        };

        return View(model);
    }

    //old index, not in use anymore. Replaced by index above ^
    public async Task<IActionResult> OldIndex()
    {
        //fetching users from external database
        var users = await _context.Users.ToListAsync();

        if (users.Any())
        {
            Console.WriteLine($"Found {users.Count} users");
            foreach (var user in users) 
            {
                Console.WriteLine($"User Found: {user.Firstname} {user.Lastname} {user.Fodselsnr} {user.Passord}");
            }
        }
        
        else
        {
            Console.WriteLine("User Not Found");
        }
        
        

        return View(users);
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