using InventorySystem.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using InventorySystem.Middleware;

// 🔽 eklendi
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext - SQL Server bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS varsa 'Always'
});

// 🔽 Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "Inventory.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;          // intranet için ideal
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // prod HTTPS: Always
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddAuthorization();

// Program.cs (yalnızca Serilog konfigürasyonu satırını genişletiyoruz)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext() // << LogContext.PushProperty(...) alanlarını enable eder
    .WriteTo.File(
        "Logs/log.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
            "{Message:lj} | Op={Op} Barcode={Barcode} User={User} DeliveredTo={DeliveredTo}{NewLine}{Exception}")
    .CreateLogger();

var app = builder.Build();

// Middleware sırası
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 🔽 Global try-catch + log
app.UseGlobalExceptionHandling();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
