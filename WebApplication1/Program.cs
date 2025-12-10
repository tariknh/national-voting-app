using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using WebApplication1.Data;
using DotNetEnv;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

//load the secret .env file to the program, encryption and all that
Env.Load();

//get the connection string from the .env file
var connectionString = Env.GetString("CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string not found in .env");
// Enable console logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<VotingTokenService>();

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

    var configuredRedirect = builder.Configuration["Criipto:RedirectUri"];
    var configuredLogoutRedirect = builder.Configuration["Criipto:PostLogoutRedirectUri"];

    if (!string.IsNullOrEmpty(configuredRedirect))
    {
        var callbackPath = new PathString(new Uri(configuredRedirect).AbsolutePath);
        options.CallbackPath = callbackPath;
    }
    else
    {
        options.CallbackPath = new PathString("/signin-oidc");
    }

    options.SignedOutCallbackPath = "/signout";

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    options.SaveTokens = true; // <-- save tokens to cookie
    options.GetClaimsFromUserInfoEndpoint = true; // <-- fetch extra claims

    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";


    options.ClaimActions.MapUniqueJsonKey("email", "email");
    options.ClaimActions.MapUniqueJsonKey("phone_number", "phone_number");

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            // --- Step 1: Log the incoming claims for debugging ---
            var principalName = context.Principal?.Identity?.Name ?? "(ukjent bruker)";
            Console.WriteLine($"✅ Token validated for {principalName}");
            foreach (var claim in context.Principal?.Claims ?? Array.Empty<Claim>())
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            // --- Step 2: Normalize provider-specific claims to .NET standard claims ---
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                var subClaim = identity.FindFirst("sub"); // OIDC standard subject claim
                if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                    Console.WriteLine($"✅ Mapped 'sub' claim to '{ClaimTypes.NameIdentifier}'");
                }
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
            if (!string.IsNullOrEmpty(configuredRedirect))
            {
                context.ProtocolMessage.RedirectUri = configuredRedirect;
            }
            Console.WriteLine("Redirecting to Criipto...");
            return Task.CompletedTask;
        },

        OnRedirectToIdentityProviderForSignOut = context =>
        {
            if (!string.IsNullOrEmpty(configuredLogoutRedirect))
            {
                context.ProtocolMessage.PostLogoutRedirectUri = configuredLogoutRedirect;
            }
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

// Configure the HTTP request pipeline.
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
