using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TowFinder.Models;
using TowFinder.Data;


var builder = WebApplication.CreateBuilder(args);

// Servisleri ekleyin.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();  // Session ekleyin

// DbContext'i MySQL ile yap�land�r�n
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 23))));

// Identity ve Cookie Authentication ekleyin
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/TowOperator/Login";
    options.AccessDeniedPath = "/TowOperator/Login";
});

var app = builder.Build();

// Rol ve admin kullan�c� ekleyin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try // Hata olu�ursa uygulamay� durdurma
    {
        await SeedAdminUser(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        throw;
    }
}

// HTTP iste�i yap�land�rmas�n�
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Authentication kulla
app.UseAuthorization();
app.UseSession();  // Session kullan

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

async Task SeedAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin")) // Admin rol� yoksa olu�tur
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    if (!await roleManager.RoleExistsAsync("TowOperator")) // TowOperator rol� yoksa olu�tur
    {
        await roleManager.CreateAsync(new IdentityRole("TowOperator"));
    }

    var adminUser = new IdentityUser
    {
        UserName = "admin",
        Email = "admin@towfinder.com",
        EmailConfirmed = true
    };

    var user = await userManager.FindByNameAsync(adminUser.UserName);
    if (user == null) // Kullan�c� yoksa olu�tur
    {
        var result = await userManager.CreateAsync(adminUser, "Admin123!"); // Admin123! �ifresini kullan
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin"); // Admin rol�n� ata
        }
    }
    else // Kullan�c� varsa rol� kontrol et
    {
        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}


