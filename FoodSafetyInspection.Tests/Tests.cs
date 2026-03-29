using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using FoodSafety.MVC.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace FoodSafety.Tests;

public class Tests
{
    private static ApplicationDbContext CreateCtx() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static ITempDataDictionary MakeTempData() =>
        new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());


    private static async Task<(Premises premises, Inspection inspection)> SeedBasicAsync(ApplicationDbContext ctx)
    {
        var premises = new Premises { Name = "Test Café", Address = "1 Main St", Town = "Cork", RiskRating = RiskRating.High };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var inspection = new Inspection
        {
            PremisesId = premises.Id,
            InspectionDate = DateTime.Today.AddDays(-10),
            Score = 40,
            Outcome = InspectionOutcome.Fail
        };
        ctx.Inspections.Add(inspection);
        await ctx.SaveChangesAsync();

        return (premises, inspection);
    }


    [Fact]
    public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        ctx.FollowUps.AddRange(
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-5), Status = FollowUpStatus.Open },
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(5), Status = FollowUpStatus.Open },
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-3), Status = FollowUpStatus.Closed }
        );
        await ctx.SaveChangesAsync();

        var overdue = await ctx.FollowUps
            .Where(f => f.Status == FollowUpStatus.Open && f.DueDate < DateTime.Today)
            .ToListAsync();

        Assert.Single(overdue);
    }

    [Fact]
    public async Task ClosingFollowUp_SetsClosedDate()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        var followUp = new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(7), Status = FollowUpStatus.Open };
        ctx.FollowUps.Add(followUp);
        await ctx.SaveChangesAsync();

        followUp.Status = FollowUpStatus.Closed;
        followUp.ClosedDate = DateTime.Today;
        await ctx.SaveChangesAsync();

        var saved = await ctx.FollowUps.FindAsync(followUp.Id);
        Assert.Equal(FollowUpStatus.Closed, saved!.Status);
        Assert.NotNull(saved.ClosedDate);
    }

    [Fact]
    public async Task DashboardCount_InspectionsThisMonth_IsCorrect()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Galway", RiskRating = RiskRating.Medium };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        ctx.Inspections.AddRange(
            new Inspection { PremisesId = premises.Id, InspectionDate = today, Score = 80, Outcome = InspectionOutcome.Pass },
            new Inspection { PremisesId = premises.Id, InspectionDate = today.AddDays(-2), Score = 45, Outcome = InspectionOutcome.Fail },
            new Inspection { PremisesId = premises.Id, InspectionDate = monthStart.AddMonths(-1), Score = 70, Outcome = InspectionOutcome.Pass }
        );
        await ctx.SaveChangesAsync();

        var count = await ctx.Inspections.CountAsync(i => i.InspectionDate >= monthStart);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task FollowUp_DueDateBeforeInspectionDate_IsInvalid()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        var followUpDueDate = DateTime.Today.AddDays(-15);
        var isInvalid = followUpDueDate < inspection.InspectionDate;

        Assert.True(isInvalid);
    }


    [Fact]
    public async Task PremisesController_Index_ReturnsAllPremises()
    {
        await using var ctx = CreateCtx();
        ctx.Premises.AddRange(
            new Premises { Name = "A", Address = "1 St", Town = "Cork", RiskRating = RiskRating.Low },
            new Premises { Name = "B", Address = "2 St", Town = "Dublin", RiskRating = RiskRating.High }
        );
        await ctx.SaveChangesAsync();

        var controller = new PremisesController(ctx);
        var result = await controller.Index() as ViewResult;

        var model = Assert.IsAssignableFrom<IEnumerable<Premises>>(result!.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task PremisesController_Details_ReturnsNotFound_WhenMissing()
    {
        await using var ctx = CreateCtx();
        var controller = new PremisesController(ctx);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PremisesController_Details_ReturnsPremises_WhenExists()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "Test", Address = "1 St", Town = "Cork", RiskRating = RiskRating.Low };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var controller = new PremisesController(ctx);
        var result = await controller.Details(premises.Id) as ViewResult;

        Assert.NotNull(result);
        Assert.IsType<Premises>(result.Model);
    }

    [Fact]
    public async Task PremisesController_Create_Post_AddsAndRedirects()
    {
        await using var ctx = CreateCtx();
        var controller = new PremisesController(ctx);

        var premises = new Premises { Name = "New Place", Address = "3 Lane", Town = "Galway", RiskRating = RiskRating.Medium };
        var result = await controller.Create(premises) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        Assert.Equal(1, await ctx.Premises.CountAsync());
    }

    [Fact]
    public async Task PremisesController_Delete_WithInspections_ShowsError()
    {
        await using var ctx = CreateCtx();
        var (premises, _) = await SeedBasicAsync(ctx);

        var controller = new PremisesController(ctx);
        controller.TempData = MakeTempData();
        var result = await controller.DeleteConfirmed(premises.Id) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        Assert.Equal(1, await ctx.Premises.CountAsync());
    }

    [Fact]
    public async Task PremisesController_Delete_WithNoInspections_Deletes()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "Empty", Address = "0 St", Town = "Dublin", RiskRating = RiskRating.Low };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var controller = new PremisesController(ctx);
        await controller.DeleteConfirmed(premises.Id);

        Assert.Equal(0, await ctx.Premises.CountAsync());
    }


    [Fact]
    public async Task InspectionsController_Index_ReturnsAllInspections()
    {
        await using var ctx = CreateCtx();
        var (_, _) = await SeedBasicAsync(ctx);

        var controller = new InspectionsController(ctx);
        var result = await controller.Index() as ViewResult;

        var model = Assert.IsAssignableFrom<IEnumerable<Inspection>>(result!.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task InspectionsController_Details_ReturnsNotFound_WhenMissing()
    {
        await using var ctx = CreateCtx();
        var controller = new InspectionsController(ctx);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task InspectionsController_Create_Post_ValidModel_Redirects()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Cork", RiskRating = RiskRating.Low };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var controller = new InspectionsController(ctx);
        var inspection = new Inspection
        {
            PremisesId = premises.Id,
            InspectionDate = DateTime.Today,
            Score = 75,
            Outcome = InspectionOutcome.Pass,
            Notes = "All good"
        };

        var result = await controller.Create(inspection) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        Assert.Equal(1, await ctx.Inspections.CountAsync());
    }

    [Fact]
    public async Task InspectionsController_Delete_RemovesInspection()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        var controller = new InspectionsController(ctx);
        await controller.DeleteConfirmed(inspection.Id);

        Assert.Equal(0, await ctx.Inspections.CountAsync());
    }


    [Fact]
    public async Task FollowUpsController_Index_ReturnsFollowUps()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);
        ctx.FollowUps.Add(new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(7), Status = FollowUpStatus.Open });
        await ctx.SaveChangesAsync();

        var controller = new FollowUpsController(ctx);
        var result = await controller.Index() as ViewResult;

        var model = Assert.IsAssignableFrom<IEnumerable<FollowUp>>(result!.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task FollowUpsController_Close_ClosesFollowUp()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);
        var followUp = new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(7), Status = FollowUpStatus.Open };
        ctx.FollowUps.Add(followUp);
        await ctx.SaveChangesAsync();

        var controller = new FollowUpsController(ctx);
        controller.TempData = MakeTempData();
        var result = await controller.Close(followUp.Id) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        var saved = await ctx.FollowUps.FindAsync(followUp.Id);
        Assert.Equal(FollowUpStatus.Closed, saved!.Status);
        Assert.NotNull(saved.ClosedDate);
    }

    [Fact]
    public async Task FollowUpsController_Close_AlreadyClosed_Redirects()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);
        var followUp = new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-5), Status = FollowUpStatus.Closed, ClosedDate = DateTime.Today.AddDays(-1) };
        ctx.FollowUps.Add(followUp);
        await ctx.SaveChangesAsync();

        var controller = new FollowUpsController(ctx);
        controller.TempData = MakeTempData();
        var result = await controller.Close(followUp.Id) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
    }

    [Fact]
    public async Task FollowUpsController_Close_NotFound_Returns404()
    {
        await using var ctx = CreateCtx();
        var controller = new FollowUpsController(ctx);

        var result = await controller.Close(999);

        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task DashboardController_Index_NoFilter_ReturnsCorrectCounts()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Cork", RiskRating = RiskRating.High };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var insp1 = new Inspection { PremisesId = premises.Id, InspectionDate = today, Score = 40, Outcome = InspectionOutcome.Fail };
        var insp2 = new Inspection { PremisesId = premises.Id, InspectionDate = today.AddDays(-2), Score = 80, Outcome = InspectionOutcome.Pass };
        var insp3 = new Inspection { PremisesId = premises.Id, InspectionDate = monthStart.AddMonths(-1), Score = 60, Outcome = InspectionOutcome.Pass };
        ctx.Inspections.AddRange(insp1, insp2, insp3);
        await ctx.SaveChangesAsync();

        ctx.FollowUps.AddRange(
            new FollowUp { InspectionId = insp1.Id, DueDate = today.AddDays(-3), Status = FollowUpStatus.Open },  // overdue
            new FollowUp { InspectionId = insp1.Id, DueDate = today.AddDays(5), Status = FollowUpStatus.Open }   // not overdue
        );
        await ctx.SaveChangesAsync();

        var controller = new DashboardController(ctx);
        var result = await controller.Index(null, null) as ViewResult;
        var vm = Assert.IsType<FoodSafetyInspection.MVC.Models.DashboardViewModel>(result!.Model);

        Assert.Equal(2, vm.InspectionsThisMonth);
        Assert.Equal(1, vm.FailedThisMonth);
        Assert.Equal(1, vm.OverdueFollowUps);
    }

    [Fact]
    public async Task DashboardController_Index_FilterByTown_ReturnsFilteredCounts()
    {
        await using var ctx = CreateCtx();
        var corkPremises = new Premises { Name = "Cork Place", Address = "1 St", Town = "Cork", RiskRating = RiskRating.Low };
        var dublinPremises = new Premises { Name = "Dublin Place", Address = "2 St", Town = "Dublin", RiskRating = RiskRating.High };
        ctx.Premises.AddRange(corkPremises, dublinPremises);
        await ctx.SaveChangesAsync();

        var today = DateTime.Today;
        ctx.Inspections.AddRange(
            new Inspection { PremisesId = corkPremises.Id, InspectionDate = today, Score = 50, Outcome = InspectionOutcome.Fail },
            new Inspection { PremisesId = dublinPremises.Id, InspectionDate = today, Score = 90, Outcome = InspectionOutcome.Pass }
        );
        await ctx.SaveChangesAsync();

        var controller = new DashboardController(ctx);
        var result = await controller.Index("Cork", null) as ViewResult;
        var vm = Assert.IsType<FoodSafetyInspection.MVC.Models.DashboardViewModel>(result!.Model);

        Assert.Equal(1, vm.InspectionsThisMonth);
        Assert.Equal(1, vm.FailedThisMonth);
    }

    [Fact]
    public async Task DashboardController_Index_FilterByRiskRating_ReturnsFilteredCounts()
    {
        await using var ctx = CreateCtx();
        var highRisk = new Premises { Name = "High Risk", Address = "1 St", Town = "Cork", RiskRating = RiskRating.High };
        var lowRisk = new Premises { Name = "Low Risk", Address = "2 St", Town = "Cork", RiskRating = RiskRating.Low };
        ctx.Premises.AddRange(highRisk, lowRisk);
        await ctx.SaveChangesAsync();

        var today = DateTime.Today;
        ctx.Inspections.AddRange(
            new Inspection { PremisesId = highRisk.Id, InspectionDate = today, Score = 30, Outcome = InspectionOutcome.Fail },
            new Inspection { PremisesId = lowRisk.Id, InspectionDate = today, Score = 85, Outcome = InspectionOutcome.Pass }
        );
        await ctx.SaveChangesAsync();

        var controller = new DashboardController(ctx);
        var result = await controller.Index(null, RiskRating.High) as ViewResult;
        var vm = Assert.IsType<FoodSafetyInspection.MVC.Models.DashboardViewModel>(result!.Model);

        Assert.Equal(1, vm.InspectionsThisMonth);
        Assert.Equal(1, vm.FailedThisMonth);
    }


    [Fact]
    public async Task FollowUp_WithNoClosedDate_RemainsOpen()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        var followUp = new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(7), Status = FollowUpStatus.Open, ClosedDate = null };
        ctx.FollowUps.Add(followUp);
        await ctx.SaveChangesAsync();

        var saved = await ctx.FollowUps.FindAsync(followUp.Id);
        Assert.Equal(FollowUpStatus.Open, saved!.Status);
        Assert.Null(saved.ClosedDate);
    }

    [Fact]
    public async Task Inspection_FailOutcome_WhenScoreLow()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Cork", RiskRating = RiskRating.High };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var inspection = new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today, Score = 35, Outcome = InspectionOutcome.Fail };
        ctx.Inspections.Add(inspection);
        await ctx.SaveChangesAsync();

        var saved = await ctx.Inspections.FindAsync(inspection.Id);
        Assert.Equal(InspectionOutcome.Fail, saved!.Outcome);
    }

    [Fact]
    public async Task MultipleFollowUps_SameInspection_AllOverdue_CountedCorrectly()
    {
        await using var ctx = CreateCtx();
        var (_, inspection) = await SeedBasicAsync(ctx);

        ctx.FollowUps.AddRange(
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-1), Status = FollowUpStatus.Open },
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-5), Status = FollowUpStatus.Open },
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-10), Status = FollowUpStatus.Open }
        );
        await ctx.SaveChangesAsync();

        var overdueCount = await ctx.FollowUps
            .CountAsync(f => f.Status == FollowUpStatus.Open && f.DueDate < DateTime.Today);

        Assert.Equal(3, overdueCount);
    }
}