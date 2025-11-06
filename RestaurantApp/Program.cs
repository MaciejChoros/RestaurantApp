using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
 options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => { options.SignIn.RequireConfirmedAccount = false; })
 .AddEntityFrameworkStores<ApplicationDbContext>()
 .AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var rola = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var user = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!rola.RoleExistsAsync("Admin").Result)
    {
        rola.CreateAsync(new IdentityRole("Admin")).Wait();
    }
    var admin = user.FindByNameAsync("admin").Result;
    if (admin == null)
    {
        admin = new IdentityUser
        {
            UserName = "admin",
            Email = null,
            EmailConfirmed = true
        };

        var result = user.CreateAsync(admin, "Admin123!").Result;

        if (result.Succeeded)
        {
            user.AddToRoleAsync(admin, "Admin").Wait();


        }
    }
}
    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dishes}/{action=Index}/{id?}");

    

app.Run();