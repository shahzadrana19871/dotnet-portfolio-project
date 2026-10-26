using MediCubes.Web.Data;
using MediCubes.Web.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<HospitalDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Hospital")));

var app = builder.Build();

// Demo convenience: create the schema and seed sample data. Use EF Core migrations in production.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
    db.Database.EnsureCreated();

    if (!db.Medicines.Any())
    {
        db.Medicines.AddRange(
            new Medicine { Name = "Paracetamol 500mg", Sku = "PCM-500", Quantity = 240, ReorderLevel = 100, UnitPrice = 0.10m, ExpiryDate = DateTime.Today.AddMonths(18) },
            new Medicine { Name = "Amoxicillin 250mg", Sku = "AMX-250", Quantity = 35, ReorderLevel = 50, UnitPrice = 0.45m, ExpiryDate = DateTime.Today.AddMonths(9) },
            new Medicine { Name = "Insulin Glargine", Sku = "INS-GLA", Quantity = 12, ReorderLevel = 20, UnitPrice = 24.00m, ExpiryDate = DateTime.Today.AddMonths(4) });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Medicines}/{action=Index}/{id?}");

app.Run();
