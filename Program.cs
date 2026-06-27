using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;
using StokTakip.Services;

var builder = WebApplication.CreateBuilder(args);

CookieSecurePolicy secureCookiePolicy =
    builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

builder.Services.AddControllersWithViews(options =>
{
    var policy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IProductVisibilityService, ProductVisibilityService>();

builder.Services.AddScoped<IPasswordHasher<User>, BCryptPasswordHasher>();

builder.Services
    .AddIdentity<User, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(8);

        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CompanyOwner",
        policy => policy.RequireRole(RoleNames.Owner));

    options.AddPolicy(
        "CanManageProducts",
        policy => policy.RequireRole(
            RoleNames.Owner,
            RoleNames.Admin,
            RoleNames.Personel));

    options.AddPolicy(
        "CanViewReports",
        policy => policy.RequireRole(
            RoleNames.Owner,
            RoleNames.Admin));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-StokNova.Auth";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = secureCookiePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = secureCookiePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "__Host-StokNova.Session";
    options.Cookie.Path = "/";
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-StokNova.AntiForgery";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = secureCookiePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginPolicy", context =>
    {
        string ip =
            context.Connection.RemoteIpAddress?.ToString() ??
            "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"login:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            string ip =
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                $"global:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
});

var app = builder.Build();

await EnsureIdentityRoles(app);
await EnsureSeedAdmin(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/Home/Error");

app.MapGet("/robots.txt", async context =>
{
    context.Response.ContentType = "text/plain; charset=utf-8";

    await context.Response.WriteAsync(
@"User-agent: *
Allow: /

Disallow: /Admin

Sitemap: https://stoknova.com.tr/sitemap.xml");
})
.AllowAnonymous();

app.MapGet("/sitemap.xml", async context =>
{
    context.Response.ContentType = "application/xml; charset=utf-8";

    string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

    await context.Response.WriteAsync(
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <url>
    <loc>https://stoknova.com.tr/</loc>
    <lastmod>{today}</lastmod>
    <changefreq>weekly</changefreq>
    <priority>1.0</priority>
  </url>
</urlset>");
})
.AllowAnonymous();

app.MapControllerRoute(
    name: "controller-default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static async Task EnsureIdentityRoles(WebApplication app)
{
    await using AsyncServiceScope scope =
        app.Services.CreateAsyncScope();

    try
    {
        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (string role in new[]
        {
            RoleNames.Owner,
            RoleNames.Admin,
            RoleNames.Personel
        })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
    catch
    {
        // Database may be mid-migration on first deployment; EF migration/update handles role tables.
    }
}

static async Task EnsureSeedAdmin(WebApplication app)
{
    await using AsyncServiceScope scope =
        app.Services.CreateAsyncScope();

    var logger =
        scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedData");

    try
    {
        var context =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        bool hasOwner =
        await context.Users
        .IgnoreQueryFilters()
        .AnyAsync();

        if (hasOwner)
        {
            return;
        }

        string? password =
            app.Configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            if (!app.Environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Seed admin oluşturulmadı. SeedAdmin:Password yapılandırması zorunludur.");

                return;
            }

            password = "Degistirilecek123!";
            logger.LogWarning(
                "Development ortamında varsayılan seed admin şifresi kullanıldı. İlk girişten sonra değiştirin.");
        }

        var company = new Company
        {
            CompanyName = "StokNova Yönetim",
            CompanyCode = "STOKNOVA",
            CreatedDate = DateTime.Now,
            PlanType = PlanType.Enterprise.ToString()
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var admin = new User
        {
            FullName = "StokNova Owner",
            Username = "admin@stoknova.com",
            UserName = "admin@stoknova.com",
            Email = "admin@stoknova.com",
            EmailConfirmed = true,
            Role = RoleNames.Owner,
            CompanyId = company.Id,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        IdentityResult createResult =
            await userManager.CreateAsync(admin, password);

        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Seed admin oluşturulamadı: {Errors}",
                string.Join(" ", createResult.Errors.Select(x => x.Description)));

            return;
        }

        await userManager.AddToRoleAsync(admin, RoleNames.Owner);

        context.Logs.Add(new Log
        {
            Islem = "İlk sistem yöneticisi otomatik oluşturuldu.",
            Tarih = DateTime.Now,
            CompanyId = company.Id,
            UserId = admin.Id,
            KullaniciAdi = admin.Username,
            Rol = admin.Role
        });

        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "Seed admin işlemi çalıştırılamadı. Veritabanı migration bekliyor olabilir.");
    }
}
