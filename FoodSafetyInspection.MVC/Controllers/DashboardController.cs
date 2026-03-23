
using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using FoodSafetyInspection.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FoodSafety.MVC.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? town, RiskRating? riskRating)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        IQueryable<Inspection> inspQuery = context.Inspections.Include(i => i.Premises);

        if (!string.IsNullOrWhiteSpace(town))
            inspQuery = inspQuery.Where(i => i.Premises.Town == town);

        if (riskRating.HasValue)
            inspQuery = inspQuery.Where(i => i.Premises.RiskRating == riskRating.Value);

        var inspThisMonth = await inspQuery.CountAsync(i => i.InspectionDate >= monthStart);
        var failsThisMonth = await inspQuery.CountAsync(i => i.InspectionDate >= monthStart && i.Outcome == InspectionOutcome.Fail);

        IQueryable<FollowUp> fuQuery = context.FollowUps.Include(f => f.Inspection).ThenInclude(i => i.Premises);

        if (!string.IsNullOrWhiteSpace(town))
            fuQuery = fuQuery.Where(f => f.Inspection.Premises.Town == town);
        if (riskRating.HasValue)
            fuQuery = fuQuery.Where(f => f.Inspection.Premises.RiskRating == riskRating.Value);

        var overdueFollowUps = await fuQuery
            .CountAsync(f => f.Status == FollowUpStatus.Open && f.DueDate < today);

        var towns = await context.Premises.Select(p => p.Town).Distinct().OrderBy(t => t).ToListAsync();

        Log.Information("Dashboard accessed. Town={Town} Risk={Risk} InspThisMonth={Count}", town, riskRating, inspThisMonth);

        var vm = new DashboardViewModel
        {
            InspectionsThisMonth = inspThisMonth,
            FailedThisMonth = failsThisMonth,
            OverdueFollowUps = overdueFollowUps,
            FilterTown = town,
            FilterRiskRating = riskRating,
            Towns = towns,
        };

        return View(vm);
    }
}