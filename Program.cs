using InventorySystem.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

// Uygulamanın yapılandırılmasını başlatıyoruz
var builder = WebApplication.CreateBuilder(args);

// 1️⃣ MVC controller ve view servislerini ekliyoruz (standart)
builder.Services.AddControllersWithViews();

// 2️⃣ Veritabanı bağlantısı için DbContext'i, connection string ile ekliyoruz
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3️⃣ HttpContext'e erişim için accessor eklenir (bazı helper sınıflarda işe yarar)
builder.Services.AddHttpContextAccessor();

// 4️⃣ Serilog ile günlük (log) tutma ayarı
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// 5️⃣ **Session servisini ekliyoruz**
// Session, kullanıcıya özel geçici veri saklamak için kullanılır (örn: login olan admin'in bilgisini veya alışveriş sepetini session'da tutarsın).
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 dakika boyunca işlem yapılmazsa session biter
    options.Cookie.HttpOnly = true;                 // Cookie'ye sadece sunucu erişebilir (daha güvenli)
    options.Cookie.IsEssential = true;              // Kullanıcı çerezleri reddetse bile session çerezi mutlaka olmalı
});

var app = builder.Build();

// 6️⃣ HTTP isteği pipeline'ı yapılandırıyoruz
if (!app.Environment.IsDevelopment())
{
    // Hata durumunda özel error sayfası ve HSTS aktif
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Tüm istekleri HTTPS'e yönlendir
app.UseStaticFiles();      // wwwroot altındaki statik dosyalara (css, js, img) izin ver

app.UseRouting();          // Route'ları aktifleştir

app.UseSession();          // **Session middleware mutlaka Routing'den sonra ve Authorization'dan önce çağrılmalı!**

app.UseAuthorization();    // Yetkilendirme kontrolleri (login olup olmama)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

// Uygulamayı başlatıyoruz
app.Run();

/*
    --- SESSION NEDİR? ---

    - Session, sunucu tarafında, kullanıcıya özel kısa süreli veri saklama yöntemidir.
    - Örneğin: Bir kullanıcı giriş yaptıysa onun "admin" olup olmadığını, session'da tutabilirsin.
    - Session verisi, kullanıcının tarayıcısındaki bir session cookie ile eşleştirilir.
    - Her istekle beraber sunucuya "ben buyum" diye gelir ve sunucu onun verisini bulur.

    --- BU PROJEDE NEDEN KULLANIYORUZ? ---

    - Sadece adminlerin ürün eklemesini/silmesini/incelemesini istiyorsun.
    - Kullanıcı login olunca, `HttpContext.Session.SetString("IsAdmin", "true")` diyorsun.
    - Sonra her action başında, gerçekten admin mi diye `Session.GetString("IsAdmin")` ile kontrol ediyorsun.
    - Eğer admin değilse -> login ekranına yönlendiriyorsun.
    - Session olmazsa, kim admin, kim değil asla ayırt edemezsin!

    --- DİKKAT ETMEN GEREKENLER ---
    - Session servislerini mutlaka ekle ve kullan.
    - Cookie'lerin engellenmediğine emin ol (yoksa session kaybolur).
    - Session, login/logout işlemlerinin güvenli çalışması için şarttır.
*/
