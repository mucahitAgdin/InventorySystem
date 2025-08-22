// Program.cs
using InventorySystem.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using InventorySystem.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

// Localization için gerekli using’ler
using Microsoft.AspNetCore.Localization;
using System.Globalization;

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

// -----------------  Localization Servisleri -----------------
// Resources klasörünün kökünü bildiriyoruz.
//  - .resx dosyaları “/Resources” altında olacak (Controllers, Views alt ayrımlarını orada yapacağız).
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Desteklenen kültürleri tanımla (UI + formatlar)
//  - "tr" default; "en", "fr" opsiyonları
var supportedCultures = new[] { "tr", "en", "fr" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture("tr"); // varsayılan
    options.SupportedCultures = cultures;    // sayı/tarih formatları
    options.SupportedUICultures = cultures;  // UI metinleri (resx)
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
