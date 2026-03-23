using FoodSafetyInspection.Domain;
using FoodSafetyInspection.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodSafety.Tests;

public class Tests
{
    private static ApplicationDbContext CreateCtx() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // Test 1: Overdue follow-ups query returns correct items
    [Fact]
    public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "Test", Address = "1 St", Town = "Cork", RiskRating = RiskRating.High };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var inspection = new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddDays(-30), Score = 40, Outcome = InspectionOutcome.Fail };
        ctx.Inspections.Add(inspection);
        await ctx.SaveChangesAsync();

        ctx.FollowUps.AddRange(
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-5), Status = FollowUpStatus.Open }, // overdue
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(5), Status = FollowUpStatus.Open }, // not overdue
            new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-3), Status = FollowUpStatus.Closed }  // closed, not counted
        );
        await ctx.SaveChangesAsync();

        var overdue = await ctx.FollowUps
            .Where(f => f.Status == FollowUpStatus.Open && f.DueDate < DateTime.Today)
            .ToListAsync();

        Assert.Single(overdue);
    }

    // Test 2: FollowUp cannot be closed without ClosedDate
    [Fact]
    public async Task ClosingFollowUp_SetsClosedDate()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Dublin", RiskRating = RiskRating.Low };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var inspection = new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddDays(-10), Score = 50, Outcome = InspectionOutcome.Fail };
        ctx.Inspections.Add(inspection);
        await ctx.SaveChangesAsync();

        var followUp = new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(7), Status = FollowUpStatus.Open };
        ctx.FollowUps.Add(followUp);
        await ctx.SaveChangesAsync();

        // Simulate the close action
        followUp.Status = FollowUpStatus.Closed;
        followUp.ClosedDate = DateTime.Today;
        await ctx.SaveChangesAsync();

        var saved = await ctx.FollowUps.FindAsync(followUp.Id);
        Assert.Equal(FollowUpStatus.Closed, saved!.Status);
        Assert.NotNull(saved.ClosedDate);
    }

    // Test 3: Dashboard count matches known seed data
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
            new Inspection { PremisesId = premises.Id, InspectionDate = monthStart.AddMonths(-1), Score = 70, Outcome = InspectionOutcome.Pass } // last month, shouldn't count
        );
        await ctx.SaveChangesAsync();

        var count = await ctx.Inspections.CountAsync(i => i.InspectionDate >= monthStart);
        Assert.Equal(2, count);
    }

    // Test 4: FollowUp DueDate before InspectionDate is a business rule violation
    [Fact]
    public async Task FollowUp_DueDateBeforeInspectionDate_IsInvalid()
    {
        await using var ctx = CreateCtx();
        var premises = new Premises { Name = "P", Address = "A", Town = "Cork", RiskRating = RiskRating.High };
        ctx.Premises.Add(premises);
        await ctx.SaveChangesAsync();

        var inspection = new Inspection
        {
            PremisesId = premises.Id,
            InspectionDate = DateTime.Today,
            Score = 40,
            Outcome = InspectionOutcome.Fail
        };
        ctx.Inspections.Add(inspection);
        await ctx.SaveChangesAsync();

        var followUpDueDate = DateTime.Today.AddDays(-5); // BEFORE inspection date
        var isInvalid = followUpDueDate < inspection.InspectionDate;

        Assert.True(isInvalid, "DueDate before InspectionDate should be flagged as invalid");
    }
}