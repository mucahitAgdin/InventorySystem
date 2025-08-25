// Program.cs
using InventorySystem.Data;
using InventorySystem.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using static Microsoft.AspNetCore.Localization.CookieRequestCultureProvider;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;


var supportedCultures = new[] { "tr", "en" /* "fr" ileride */ };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture("tr");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    // 🔽 Kullanıcı seçimi (cookie) > querystring > Accept-Language
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new CookieRequestCultureProvider(),        // .AspNetCore.Culture
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var builder = WebApplication.CreateBuilder(args);

// ----------------- MVC -----------------
builder.Services.AddControllersWithViews()

    // View seviyesinde IStringLocalizer kullanabilmek için
    //   - _Layout, Razor View’lar ve TagHelper’larda @Localizer["Key"] yazabileceğiz
    .AddViewLocalization()

    // DataAnnotations (Model doğrulama mesajları) yerelleştirme
    //   - [Required], [StringLength] gibi attribute hata mesajları .resx’ten gelecek
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // prod HTTPS: Always
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
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
            "{Message:lj} | Op={Op} Barcode={Barcode} User={User} DeliveredTo={DeliveredTo}{NewLine}{Exception}")
    .CreateLogger();


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

// Localization middleware tam burada olmalı.
//  - Routing’ten sonra, Session/Auth/Authorization’dan önce.
app.UseRequestLocalization();

// Oturum ve kimlik doğrulama
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Global try-catch + log
app.UseGlobalExceptionHandling();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
