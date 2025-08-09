using InventorySystem.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

// 🔽 yeni eklemeler
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// Serilog (sizde vardı)
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Antiforgery (siz eklemiştiniz – kalsın)
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = ".AspNetCore.Antiforgery";
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// ❌ Session’ı ARTIK kullanmıyoruz (isteğe bağlı kaldırabilirsiniz).
// builder.Services.AddSession(...);

// ✅ Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.SlidingExpiration = true;

        // Mixed-scheme sorunlarını minimuma indir
        options.Cookie.Name = ".Inventory.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CookiePolicy: güvenli bayrakları zorlayalım
app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always,
    MinimumSameSitePolicy = SameSiteMode.Lax
});

// ✅ kimlik doğrulama / yetkilendirme (Session yerine bunlar)
app.UseAuthentication();
app.UseAuthorization();

// (İsteğe bağlı) http alt kaynakları upgrade et
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] = "upgrade-insecure-requests";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
