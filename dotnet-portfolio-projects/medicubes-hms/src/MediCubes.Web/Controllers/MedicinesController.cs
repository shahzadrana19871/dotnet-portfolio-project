using MediCubes.Web.Data;
using MediCubes.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCubes.Web.Controllers;

public class MedicinesController(HospitalDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool lowStock = false)
    {
        var query = db.Medicines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(m => m.Name.Contains(q) || m.Sku.Contains(q));

        if (lowStock)
            query = query.Where(m => m.Quantity <= m.ReorderLevel);

        ViewBag.Query = q;
        ViewBag.LowStock = lowStock;
        return View(await query.OrderBy(m => m.Name).ToListAsync());
    }

    public IActionResult Create() => View(new Medicine());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine medicine)
    {
        if (await db.Medicines.AnyAsync(m => m.Sku == medicine.Sku))
            ModelState.AddModelError(nameof(Medicine.Sku), "This SKU already exists.");

        if (!ModelState.IsValid) return View(medicine);

        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var medicine = await db.Medicines.FindAsync(id);
        return medicine is null ? NotFound() : View(medicine);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Medicine medicine)
    {
        if (id != medicine.Id) return BadRequest();

        if (await db.Medicines.AnyAsync(m => m.Sku == medicine.Sku && m.Id != id))
            ModelState.AddModelError(nameof(Medicine.Sku), "This SKU already exists.");

        if (!ModelState.IsValid) return View(medicine);

        db.Update(medicine);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
