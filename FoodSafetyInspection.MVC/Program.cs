using FoodSafetyInspection.MVC.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File("logs/foodsafety-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("UserName", ctx.User?.Identity?.Name ?? "anonymous");
        diag.Set("RequestHost", ctx.Request.Host.Value);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async ctx =>
        {
            var exFeature = ctx.Features.Get<IExceptionHandlerFeature>();
            if (exFeature?.Error is not null)
            {
                var userName = ctx.User?.Identity?.Name ?? "anonymous";
                Log.Error(exFeature.Error,
                    "Unhandled exception. Path={Path} User={UserName}",
                    ctx.Request.Path, userName);
            }
            ctx.Response.Redirect("/Home/Error");
            await Task.CompletedTask;
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Dashboard}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
    await SeedData.InitialiseAsync(scope.ServiceProvider);

app.Run();