
using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FoodSafety.MVC.Controllers;

[Authorize]
public class InspectionsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var inspections = await context.Inspections
            .Include(i => i.Premises)
            .OrderByDescending(i => i.InspectionDate)
            .ToListAsync();
        return View(inspections);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var inspection = await context.Inspections
            .Include(i => i.Premises)
            .Include(i => i.FollowUps)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inspection is null ? NotFound() : View(inspection);
    }

    [Authorize(Roles = "Admin,Inspector")]
    public IActionResult Create()
    {
        ViewData["PremisesId"] = new SelectList(context.Premises, "Id", "Name");
        return View(new Inspection { InspectionDate = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create([Bind("PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
    {
        if (!ModelState.IsValid)
        {
            ViewData["PremisesId"] = new SelectList(context.Premises, "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }
        context.Add(inspection);
        await context.SaveChangesAsync();
        Log.Information("Inspection created: {InspectionId} for PremisesId {PremisesId}, Outcome: {Outcome}",
            inspection.Id, inspection.PremisesId, inspection.Outcome);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var inspection = await context.Inspections.FindAsync(id);
        if (inspection is null) return NotFound();
        ViewData["PremisesId"] = new SelectList(context.Premises, "Id", "Name", inspection.PremisesId);
        return View(inspection);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
    {
        if (id != inspection.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["PremisesId"] = new SelectList(context.Premises, "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }
        try
        {
            context.Update(inspection);
            await context.SaveChangesAsync();
            Log.Information("Inspection updated: {InspectionId}", inspection.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Error(ex, "Concurrency error updating Inspection {InspectionId}", id);
            if (!context.Inspections.Any(i => i.Id == id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var inspection = await context.Inspections
            .Include(i => i.Premises)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inspection is null ? NotFound() : View(inspection);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inspection = await context.Inspections.FindAsync(id);
        if (inspection is not null)
        {
            context.Inspections.Remove(inspection);
            await context.SaveChangesAsync();
            Log.Information("Inspection deleted: {InspectionId}", id);
        }
        return RedirectToAction(nameof(Index));
    }
}