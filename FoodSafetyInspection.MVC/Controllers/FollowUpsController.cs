using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FoodSafety.MVC.Controllers;

[Authorize]
public class FollowUpsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(FollowUpStatus? status, bool overdueOnly = false, string? sortBy = null)
    {
        var today = DateTime.Today;
        var query = context.FollowUps
            .Include(f => f.Inspection).ThenInclude(i => i.Premises)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (overdueOnly)
            query = query.Where(f => f.Status == FollowUpStatus.Open && f.DueDate < today);

        query = sortBy switch
        {
            "due_desc" => query.OrderByDescending(f => f.DueDate),
            "premises" => query.OrderBy(f => f.Inspection.Premises.Name),
            "status" => query.OrderBy(f => f.Status),
            _ => query.OrderBy(f => f.DueDate),
        };

        ViewBag.FilterStatus = status;
        ViewBag.OverdueOnly = overdueOnly;
        ViewBag.SortBy = sortBy;

        return View(await query.ToListAsync());
    }

    [Authorize(Roles = "Admin,Inspector")]
    public IActionResult Create(int? inspectionId)
    {
        ViewData["InspectionId"] = new SelectList(
            context.Inspections.Include(i => i.Premises)
                .Select(i => new { i.Id, Display = i.Premises.Name + " – " + i.InspectionDate.ToShortDateString() }),
            "Id", "Display", inspectionId);

        return View(new FollowUp { InspectionId = inspectionId ?? 0, DueDate = DateTime.Today.AddDays(14) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create([Bind("InspectionId,DueDate,Status")] FollowUp followUp)
    {
        var inspection = await context.Inspections.FindAsync(followUp.InspectionId);
        if (inspection is not null && followUp.DueDate < inspection.InspectionDate)
        {
            Log.Warning("FollowUp DueDate {DueDate} is before InspectionDate {InspectionDate} for InspectionId {InspectionId}",
                followUp.DueDate, inspection.InspectionDate, followUp.InspectionId);
            ModelState.AddModelError("DueDate", "Due date cannot be before the inspection date.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["InspectionId"] = new SelectList(
                context.Inspections.Include(i => i.Premises)
                    .Select(i => new { i.Id, Display = i.Premises.Name + " – " + i.InspectionDate.ToShortDateString() }),
                "Id", "Display", followUp.InspectionId);
            return View(followUp);
        }

        context.Add(followUp);
        await context.SaveChangesAsync();
        Log.Information("FollowUp created: {FollowUpId} for InspectionId {InspectionId}, DueDate: {DueDate}",
            followUp.Id, followUp.InspectionId, followUp.DueDate);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Close(int id)
    {
        var followUp = await context.FollowUps.FindAsync(id);
        if (followUp is null) return NotFound();

        if (followUp.Status == FollowUpStatus.Closed)
        {
            Log.Warning("Attempted to close already-closed FollowUp {FollowUpId}", id);
            TempData["Error"] = "This follow-up is already closed.";
            return RedirectToAction(nameof(Index));
        }

        followUp.Status = FollowUpStatus.Closed;
        followUp.ClosedDate = DateTime.Today;
        await context.SaveChangesAsync();
        Log.Information("FollowUp closed: {FollowUpId} on {ClosedDate}", id, followUp.ClosedDate);
        return RedirectToAction(nameof(Index));
    }
}