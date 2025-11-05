using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Database
// ----------------------------
var connectionString = builder.Configuration.GetConnectionString("NeonDatabase") ??
                       throw new InvalidOperationException("Connection string 'NeonDatabase' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ----------------------------
// Session
// ----------------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;       // Required for OIDC login
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Must use HTTPS
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
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS required
})
.AddOpenIdConnect(options =>
{
    options.ClientId = builder.Configuration["Criipto:ClientId"];
    options.ClientSecret = builder.Configuration["Criipto:ClientSecret"];
    options.Authority = $"https://{builder.Configuration["Criipto:Domain"]}/";
    options.ResponseType = "code";

    options.CallbackPath = "/callback";
    options.SignedOutCallbackPath = "/signout";

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // Enable debug logging for development
    options.Events = new OpenIdConnectEvents
    {
        OnRemoteFailure = context =>
        {
            Console.WriteLine($"OIDC Error: {context.Failure?.Message}");
            context.Response.Redirect("/Home/Error");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"User logged in: {context.Principal.Identity?.Name}");
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

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
