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

        public async Task<IActionResult> Index()
        {
            // 1. Create the ViewModel and set the login status based on authentication
            var model = new HomeViewModel
            {
                IsLoggedIn = User.Identity?.IsAuthenticated ?? false
            };

            // 2. If the user is not logged in, show the public view and exit.
            if (!model.IsLoggedIn)
            {
                _logger.LogInformation("User is not authenticated. Showing logged-out view.");
                return View(model);
            }

            // --- USER IS AUTHENTICATED ---

            // 3. Get the user's unique, stable ID from the claims principal.
            // This is the 'sub' claim we mapped to NameIdentifier in Program.cs.
            var bankIdUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(bankIdUuid))
            {
                _logger.LogWarning("User is authenticated but the NameIdentifier claim is missing. Logging out.");
                // This is a rare but critical error. Forcing a logout is a safe recovery step.
                return await Logout();
            }
            
            _logger.LogInformation("Authenticated user found with BankIdUuid: {BankIdUuid}", bankIdUuid);

            // 4. Fetch the user from your database using the unique ID.
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.BankIdUuid == bankIdUuid);

            // 5. If the user doesn't exist in your database, create a new entry.
            if (dbUser == null)
            {
                _logger.LogInformation("User not found in database. Creating new user entry.");
                dbUser = new User
                {
                    BankIdUuid = bankIdUuid,
                    Firstname = User.FindFirst(ClaimTypes.GivenName)?.Value,
                    Lastname = User.FindFirst(ClaimTypes.Surname)?.Value,
                    Phonenr = User.FindFirst("phone_number")?.Value, // Assuming phone is mapped
                    Kommune = "Unknown", // The 'address.locality' claim was empty in your logs
                    HasVoted = false
                };
                _context.Users.Add(dbUser);
                await _context.SaveChangesAsync();
            }

            // 6. Populate the ViewModel with data from the claims and the database user.
            model.FullName = User.Identity?.Name;
            model.Phone = dbUser.Phonenr;
            model.Kommune = dbUser.Kommune;
            model.Fodselsnr = dbUser.Fodselsnr; // This will be null unless you set it elsewhere
            model.HasVoted = dbUser.HasVoted ?? false; // Safely handle null from the database

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Vote(string party)
        {
            // Use the stable NameIdentifier to find the user
            var bankIdUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(bankIdUuid))
            {
                return Unauthorized("User identifier not found in claims.");
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.BankIdUuid == bankIdUuid);
            if (dbUser == null)
            {
                return NotFound("User not found in the database.");
            }

            if (dbUser.HasVoted == true)
            {
                TempData["Message"] = "You have already voted.";
                return RedirectToAction("Index");
            }

            // Register the vote (logic for storing the actual vote for 'party' would go here)
            // For now, we just mark the user as having voted.
            dbUser.HasVoted = true;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Your vote has been registered successfully!";
            return RedirectToAction("Index");
        }

        // --- Authentication Actions ---

        public IActionResult Login()
        {
            // Challenge the OpenIdConnect scheme, which will trigger the redirect to Criipto.
            var redirectUri = Url.Action("Index", "Home");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
        }
        
        public async Task<IActionResult> Logout()
        {
            // Sign out from the cookie and the OIDC provider.
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // --- Standard Pages ---

        public IActionResult Privacy() => View();

        public async Task<IActionResult> Contact()
        {
            var model = new ContactViewModel
            {
                IsLoggedIn = User.Identity?.IsAuthenticated ?? false,
                FullName = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value
            };

            if (model.IsLoggedIn)
            {
                var bankIdUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(bankIdUuid))
                {
                    var dbUser = await _context.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.BankIdUuid == bankIdUuid);
                    model.Kommune = dbUser?.Kommune;
                }
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Protected() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}