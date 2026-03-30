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
    public async Task<IActionResult> Index(string? search, string? town, RiskRating? riskRating, string? sortBy)
    {
        var query = context.Premises.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{search}%") ||
                EF.Functions.Like(p.Address, $"%{search}%"));

        if (!string.IsNullOrWhiteSpace(town))
            query = query.Where(p => p.Town == town);

        if (riskRating.HasValue)
            query = query.Where(p => p.RiskRating == riskRating.Value);

        query = sortBy switch
        {
            "town" => query.OrderBy(p => p.Town),
            "risk" => query.OrderBy(p => p.RiskRating),
            "risk_desc" => query.OrderByDescending(p => p.RiskRating),
            _ => query.OrderBy(p => p.Name),
        };

        var towns = await context.Premises.Select(p => p.Town).Distinct().OrderBy(t => t).ToListAsync();

        ViewBag.Search = search;
        ViewBag.FilterTown = town;
        ViewBag.FilterRisk = riskRating;
        ViewBag.SortBy = sortBy;
        ViewBag.Towns = towns;

        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises
            .Include(p => p.Inspections)
                .ThenInclude(i => i.FollowUps)
            .FirstOrDefaultAsync(p => p.Id == id);
        return premises is null ? NotFound() : View(premises);
    }

    [Authorize(Roles = "Admin,Inspector")]
    public IActionResult Create() => View();

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

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises.FindAsync(id);
        return premises is null ? NotFound() : View(premises);
    }

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

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var premises = await context.Premises
            .Include(p => p.Inspections)
                .ThenInclude(i => i.FollowUps)
            .FirstOrDefaultAsync(p => p.Id == id);
        return premises is null ? NotFound() : View(premises);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var premises = await context.Premises
            .Include(p => p.Inspections)
                .ThenInclude(i => i.FollowUps)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (premises is null) return NotFound();

        int inspectionCount = premises.Inspections.Count;
        int followUpCount = premises.Inspections.Sum(i => i.FollowUps.Count);

        foreach (var inspection in premises.Inspections)
            context.FollowUps.RemoveRange(inspection.FollowUps);

        context.Inspections.RemoveRange(premises.Inspections);
        context.Premises.Remove(premises);

        await context.SaveChangesAsync();

        Log.Information(
            "Premises deleted (cascade): {PremisesId} {Name} — {InspectionCount} inspection(s) and {FollowUpCount} follow-up(s) also removed",
            id, premises.Name, inspectionCount, followUpCount);

        return RedirectToAction(nameof(Index));
    }
}