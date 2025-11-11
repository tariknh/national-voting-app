using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

// Enable console logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ----------------------------
// Database
// ----------------------------
var connectionString = builder.Configuration.GetConnectionString("NeonDatabase") ??
                       throw new InvalidOperationException("Connection string 'NeonDatabase' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();



builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// ----------------------------
// Session
// ----------------------------
builder.Services.AddSession(options =>
{
    
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Use Lax for better compatibility with top-level redirects.
    options.Cookie.SameSite = SameSiteMode.Lax; // <-- MODIFIED
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ----------------------------
// Authentication (Identity + Criipto BankID)
// ----------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // Use Lax for better compatibility with top-level redirects.
    options.Cookie.SameSite = SameSiteMode.Lax; // <-- MODIFIED
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(options =>
{
    options.ClientId = builder.Configuration["Criipto:ClientId"];
    options.ClientSecret = builder.Configuration["Criipto:ClientSecret"];
    options.Authority = $"https://{builder.Configuration["Criipto:Domain"]}/";
    options.ResponseType = "code";

    options.CallbackPath = new PathString("/signin-oidc");
    options.SignedOutCallbackPath = "/signout";

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    options.SaveTokens = true; // <-- save tokens to cookie
    options.GetClaimsFromUserInfoEndpoint = true; // <-- fetch extra claims

    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";


    options.ClaimActions.MapUniqueJsonKey("uniqueuserid", "sub");
    options.ClaimActions.MapUniqueJsonKey("email", "email");
    options.ClaimActions.MapUniqueJsonKey("phone_number", "phone_number");

    options.Events = new OpenIdConnectEvents
{
    OnTokenValidated = context =>
    {
        // --- Step 1: Log the incoming claims for debugging ---
        Console.WriteLine($"✅ Token validated for {context.Principal.Identity?.Name}");
        foreach (var claim in context.Principal.Claims)
        {
            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        }

        // --- Step 2: Normalize provider-specific claims to .NET standard claims ---
        if (context.Principal.Identity is ClaimsIdentity identity)
        {
            // Find the 'givenname' claim from the provider
            var givenNameClaim = identity.FindFirst("givenname");
            if (givenNameClaim != null)
            {
                // If the standard GivenName claim doesn't exist, add it
                if (!identity.HasClaim(c => c.Type == ClaimTypes.GivenName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.GivenName, givenNameClaim.Value));
                    Console.WriteLine($"✅ Added standard claim: {ClaimTypes.GivenName} = {givenNameClaim.Value}");
                }
            }

            // Find the 'surname' claim from the provider
            var surnameClaim = identity.FindFirst("surname");
            if (surnameClaim != null)
            {
                // If the standard Surname claim doesn't exist, add it
                if (!identity.HasClaim(c => c.Type == ClaimTypes.Surname))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Surname, surnameClaim.Value));
                    Console.WriteLine($"✅ Added standard claim: {ClaimTypes.Surname} = {surnameClaim.Value}");
                }
            }
        }
        
        return Task.CompletedTask;
    },

    OnRedirectToIdentityProvider = context =>
    {
        Console.WriteLine("Redirecting to Criipto...");
        return Task.CompletedTask;
    },

    OnAuthorizationCodeReceived = context =>
    {
        Console.WriteLine("Auth code received");
        return Task.CompletedTask;
    },

    OnRemoteFailure = context =>
    {
        Console.WriteLine($"❌ OIDC Error: {context.Failure?.Message}");
        context.HandleResponse();
        context.Response.Redirect("/Home/Error");
        return Task.CompletedTask;
    }
};
});
var app = builder.Build();

// ----------------------------
// HTTP request pipeline
// ----------------------------
app.UseSession();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();
