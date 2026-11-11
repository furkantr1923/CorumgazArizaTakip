using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using CorumgazArizaTakip.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var kultur = (CultureInfo)CultureInfo.GetCultureInfo("tr-TR").Clone();
kultur.NumberFormat.NumberDecimalSeparator = ".";
kultur.NumberFormat.NumberGroupSeparator = ",";
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

builder.Services.AddDbContext<ArizaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Login";
        options.LogoutPath = "/Hesap/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(kultur),
    SupportedCultures = new List<CultureInfo> { kultur },
    SupportedUICultures = new List<CultureInfo> { kultur }
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArizaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    db.Database.EnsureCreated();

    if (!db.Kullanicilar.Any())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var kullaniciAdi = config["IlkAdmin:KullaniciAdi"];
        var sifre = config["IlkAdmin:Sifre"];

        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
        {
            logger.LogWarning("İlk admin oluşturulmadı. 'IlkAdmin:KullaniciAdi' ve 'IlkAdmin:Sifre' ayarlarını Kullanıcı Gizli Anahtarları'na ekleyin.");
        }
        else
        {
            var hasher = new PasswordHasher<Kullanici>();
            var admin = new Kullanici { KullaniciAdi = kullaniciAdi };
            admin.SifreHash = hasher.HashPassword(admin, sifre);

            db.Kullanicilar.Add(admin);
            db.SaveChanges();
            logger.LogInformation("İlk admin oluşturuldu: {KullaniciAdi}", kullaniciAdi);
        }
    }

    if (!db.Teknisyenler.Any())
    {
        db.Teknisyenler.AddRange(
            new Teknisyen { AdSoyad = "Ahmet Yılmaz", Uzmanlik = "Gaz kaçağı", Bolge = "Merkez", Enlem = 40.5506, Boylam = 34.9556 },
            new Teknisyen { AdSoyad = "Mehmet Kaya", Uzmanlik = "Sayaç arızası", Bolge = "Kuzey", Enlem = 40.5700, Boylam = 34.9550 },
            new Teknisyen { AdSoyad = "Ayşe Demir", Uzmanlik = "İç tesisat", Bolge = "Güney", Enlem = 40.5330, Boylam = 34.9560 },
            new Teknisyen { AdSoyad = "Fatma Çelik", Uzmanlik = "Acil müdahale", Bolge = "Doğu", Enlem = 40.5510, Boylam = 34.9800 },
            new Teknisyen { AdSoyad = "Emre Şahin", Uzmanlik = "Gaz kaçağı", Bolge = "Batı", Enlem = 40.5500, Boylam = 34.9300 },
            new Teknisyen { AdSoyad = "Zeynep Arslan", Uzmanlik = "Sayaç arızası", Bolge = "Kuzeydoğu", Enlem = 40.5650, Boylam = 34.9750 },
            new Teknisyen { AdSoyad = "Murat Öztürk", Uzmanlik = "Servis hattı", Bolge = "Kuzeybatı", Enlem = 40.5650, Boylam = 34.9350 },
            new Teknisyen { AdSoyad = "Elif Koç", Uzmanlik = "İç tesisat", Bolge = "Güneydoğu", Enlem = 40.5370, Boylam = 34.9750 },
            new Teknisyen { AdSoyad = "Burak Aydın", Uzmanlik = "Acil müdahale", Bolge = "Güneybatı", Enlem = 40.5370, Boylam = 34.9380 },
            new Teknisyen { AdSoyad = "Selin Yıldız", Uzmanlik = "Servis hattı", Bolge = "Sanayi", Enlem = 40.5450, Boylam = 34.9950 }
        );
        db.SaveChanges();
    }
}

app.Run();