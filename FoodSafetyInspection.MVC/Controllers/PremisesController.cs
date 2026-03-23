
using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FoodSafety.MVC.Controllers;

[Authorize]
public class PremisesController(ApplicationDbContext context) : Controller
{
    // GET: Premises
    public async Task<IActionResult> Index()
        => View(await context.Premises.OrderBy(p => p.Name).ToListAsync());

    // GET: Premises/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises
            .Include(p => p.Inspections)
            .FirstOrDefaultAsync(p => p.Id == id);
        return premises is null ? NotFound() : View(premises);
    }

    // GET: Premises/Create
    [Authorize(Roles = "Admin,Inspector")]
    public IActionResult Create() => View();

    // POST: Premises/Create
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create([Bind("Name,Address,Town,RiskRating")] Premises premises)
    {
        if (!ModelState.IsValid) return View(premises);
        context.Add(premises);
        await context.SaveChangesAsync();
        Log.Information("Premises created: {PremisesId} {Name} in {Town}", premises.Id, premises.Name, premises.Town);
        return RedirectToAction(nameof(Index));
    }

    // GET: Premises/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises.FindAsync(id);
        return premises is null ? NotFound() : View(premises);
    }

    // POST: Premises/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Town,RiskRating")] Premises premises)
    {
        if (id != premises.Id) return NotFound();
        if (!ModelState.IsValid) return View(premises);
        try
        {
            context.Update(premises);
            await context.SaveChangesAsync();
            Log.Information("Premises updated: {PremisesId} {Name}", premises.Id, premises.Name);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Error(ex, "Concurrency error updating Premises {PremisesId}", id);
            if (!context.Premises.Any(p => p.Id == premises.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Premises/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises.FirstOrDefaultAsync(p => p.Id == id);
        return premises is null ? NotFound() : View(premises);
    }

    // POST: Premises/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var premises = await context.Premises
            .Include(p => p.Inspections)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (premises is null) return NotFound();
        if (premises.Inspections.Any())
        {
            TempData["Error"] = "Cannot delete premises with existing inspections.";
            return RedirectToAction(nameof(Index));
        }
        context.Premises.Remove(premises);
        await context.SaveChangesAsync();
        Log.Information("Premises deleted: {PremisesId}", id);
        return RedirectToAction(nameof(Index));
    }
}