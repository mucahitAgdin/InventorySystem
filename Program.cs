// Program.cs  — minimal, öğretici ve Admin kapsamını bozmadan
using InventorySystem.Data;
using InventorySystem.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options; // UseRequestLocalization için DI’dan options çekmek
using Serilog;
using System.Globalization;
// using System.Linq; // (genelde implicit, gerekirse aç)

var builder = WebApplication.CreateBuilder(args); // 🔴 ÖNCE builder’ı oluştur

// ----------------- MVC -----------------
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ----------------- DbContext -----------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----------------- Session -----------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // prod: Always
});

// ----------------- Cookie Authentication -----------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "Inventory.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddAuthorization();

// ----------------- Serilog -----------------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(
        "Logs/log.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} | Op={Op} Barcode={Barcode} User={User} DeliveredTo={DeliveredTo}{NewLine}{Exception}")
    .CreateLogger();

// ----------------- Localization Servisleri -----------------
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Desteklenen kültürler (UI + format)
var supportedCultures = new[] { "tr", "en" }; // şimdilik Admin için TR/EN
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture("tr");
    options.SupportedCultures = cultures;    // sayı-tarih
    options.SupportedUICultures = cultures;  // .resx UI metinleri

    // Kullanıcı seçimi öncelikli: Cookie > QueryString > Accept-Language
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var app = builder.Build();

// ----------------- Middleware Sırası -----------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Localization: DI’dan ayarları çek ve uygula
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

// Oturum ve kimlik doğrulama
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/sys-admin") && !ctx.User.Identity?.IsAuthenticated == true)
    {
        ctx.Response.StatusCode = 404; // login’e değil 404’e düşür; gizlilik
        return;
    }
    await next();
});


// Global try-catch + log (senin custom middleware’in)
app.UseGlobalExceptionHandling();

// Default route: şimdilik Admin/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
