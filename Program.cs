using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using tikthumb.Db;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// builder.Services.AddDbContextPool<DataProtectionDbContext>(options =>
// {
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DataProtection"), configure =>
//     {
//         configure.EnableRetryOnFailure();
//     });
//     options.EnableDetailedErrors();
// });

// builder.Services.AddDataProtection()
// .PersistKeysToDbContext<DataProtectionDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
