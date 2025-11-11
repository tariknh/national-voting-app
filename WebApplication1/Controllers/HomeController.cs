using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
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
            var redirectUri = Url.Action("Index", "Home");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri },
                             OpenIdConnectDefaults.AuthenticationScheme);
        }

        // ----------------------------
        // Logout
        // ----------------------------
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // ----------------------------
        // Protected test page
        // ----------------------------
        [Authorize]
        public IActionResult Protected()
        {
            return View();
        }

        // ----------------------------
        // Main page
        // ----------------------------
  
        public async Task<IActionResult> Index()
{
    // Create ViewModel
    var model = new HomeViewModel();

    // If user is not logged in, just show homepage
    if (!User.Identity?.IsAuthenticated ?? true)
    {
        Console.WriteLine("--- User is NOT Authenticated. Showing logged-out view. ---");
        return View(model);
    }
    
    // =================================================================
    // START: HEFTY LOGGING BLOCK
    // =================================================================
    Console.WriteLine("\n\n==========================================================");
    Console.WriteLine("HOME CONTROLLER: Detailed Claims Inspection");
    Console.WriteLine($"Timestamp: {DateTime.UtcNow:O}");
    Console.WriteLine("----------------------------------------------------------");
    Console.WriteLine($"User.Identity.IsAuthenticated: {User.Identity.IsAuthenticated}");
    Console.WriteLine($"User.Identity.Name: '{User.Identity.Name}'");
    Console.WriteLine($"User.Identity.AuthenticationType: {User.Identity.AuthenticationType}");

    // Inspect ALL claims available in the current principal
    var allClaims = User.Claims.ToList();
    if (allClaims.Any())
    {
        Console.WriteLine("\n--- All Claims Found in User.Claims ---");
        foreach (var claim in allClaims)
        {
            Console.WriteLine($"  -> Type: '{claim.Type}', Value: '{claim.Value}'");
        }
        Console.WriteLine("---------------------------------------\n");
    }
    else
    {
        Console.WriteLine("\n--- CRITICAL: No claims found in User.Claims collection! ---\n");
    }

    // You can also inspect multiple identities if they exist (usually there's one)
    if (User.Identity is ClaimsIdentity claimsIdentity)
    {
        Console.WriteLine("--- Claims directly from ClaimsIdentity object ---");
        foreach (var claim in claimsIdentity.Claims)
        {
             Console.WriteLine($"  -> Type: '{claim.Type}', Value: '{claim.Value}'");
        }
        Console.WriteLine("------------------------------------------------\n");
    }
    // =================================================================
    // END: HEFTY LOGGING BLOCK
    // =================================================================


    // ----------------------------
    // Read identity data from OIDC claims
    // ----------------------------
    Console.WriteLine("Attempting to find claims using standard ClaimTypes constants...");
    var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value;
    var lastName = User.FindFirst(ClaimTypes.Surname)?.Value;
    Console.WriteLine($"Value of 'firstName' (from ClaimTypes.GivenName): '{firstName ?? "NULL"}'");
    Console.WriteLine($"Value of 'lastName' (from ClaimTypes.Surname): '{lastName ?? "NULL"}'");

    Console.WriteLine("\nAttempting to find claims using short names...");
    var firstNameShort = User.FindFirst("givenname")?.Value;
    var lastNameShort = User.FindFirst("surname")?.Value;
    Console.WriteLine($"Value of 'firstNameShort' (from 'givenname'): '{firstNameShort ?? "NULL"}'");
    Console.WriteLine($"Value of 'lastNameShort' (from 'surname'): '{lastNameShort ?? "NULL"}'");


    var fullName = User.Identity?.Name ?? $"{firstName} {lastName}";
    var bankId = User.FindFirst("uniqueuserid")?.Value;
    var city = User.FindFirst("address.locality")?.Value ?? "Unknown";
    
    Console.WriteLine($"\nFinal calculated fullName: '{fullName}'");
    Console.WriteLine($"Final retrieved bankId: '{bankId}'");
    Console.WriteLine("==========================================================\n\n");

    if (string.IsNullOrEmpty(bankId))
    {
        // This is a critical failure point. If we get here, it means the 'uniqueuserid'
        // claim is missing, and the model won't be populated.
        Console.WriteLine("CRITICAL: bankId is null or empty. Returning view with empty model.");
        return View(model);
    }

    // ----------------------------
    // Fetch or create user from DB
    // ----------------------------
    var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Fodselsnr == bankId);
    if (dbUser == null)
    {
        dbUser = new User
        {
            Firstname = firstName, // This might be null if claim is not found
            Lastname = lastName,   // This might be null if claim is not found
            Fodselsnr = bankId,
            Adresse = city,
            Kommune = city,
            Email = User.FindFirst("email")?.Value,
            Phonenr = User.FindFirst("phone_number")?.Value,
        };
        _context.Users.Add(dbUser);
        await _context.SaveChangesAsync();
    }

    // ----------------------------
    // Populate ViewModel
    // ----------------------------
    model.FullName = fullName;
    model.Fodselsnr = dbUser.Fodselsnr;
    model.Email = dbUser.Email;
    model.Phone = dbUser.Phonenr;
    model.Kommune = dbUser.Kommune;
    
    // This logic seems reversed, let's fix it.
    // We should read the vote status FROM the database user TO the model.
    model.HasVoted = dbUser.HasVoted ?? true;

    // The old logic was trying to write the model's default value (false) to the dbUser.
    /*
    if (model.HasVoted)
    {
        dbUser.HasVoted = true;
    }else
    {
        if (!model.HasVoted)
        {
            dbUser.HasVoted = false;
        }
    }
    */

    return View(model);
}
        // ----------------------------
        // Register vote
        // ----------------------------
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Vote(string party)
        {
            var bankId = User.FindFirst("uniqueuserid")?.Value;
            if (string.IsNullOrEmpty(bankId)) return Unauthorized();

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Fodselsnr == bankId);
            if (dbUser == null) return NotFound();

            if (dbUser.HasVoted == true)
            {
                TempData["Message"] = "You already voted.";
                return RedirectToAction("Index");
            }

            dbUser.HasVoted = true;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Vote registered successfully!";
            return RedirectToAction("Index");
        }

        // ----------------------------
        // Privacy / Error pages
        // ----------------------------
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
