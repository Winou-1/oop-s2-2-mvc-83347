using FoodSafetyInspection.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyInspection.MVC.Data
{
    static class SeedData
    {
        public static async Task InitialiseAsync(IServiceProvider services)
        {
            var ctx = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await ctx.Database.MigrateAsync();

            foreach (var role in new[] { "Admin", "Inspector", "Viewer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // Admin
            const string adminEmail = "admin@council.ie";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin@1234");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Inspector
            const string inspectorEmail = "inspector@council.ie";
            if (await userManager.FindByEmailAsync(inspectorEmail) is null)
            {
                var inspector = new IdentityUser { UserName = inspectorEmail, Email = inspectorEmail, EmailConfirmed = true };
                await userManager.CreateAsync(inspector, "Inspector@1234");
                await userManager.AddToRoleAsync(inspector, "Inspector");
            }

            // Viewer
            const string viewerEmail = "viewer@council.ie";
            if (await userManager.FindByEmailAsync(viewerEmail) is null)
            {
                var viewer = new IdentityUser { UserName = viewerEmail, Email = viewerEmail, EmailConfirmed = true };
                await userManager.CreateAsync(viewer, "Viewer@1234");
                await userManager.AddToRoleAsync(viewer, "Viewer");
            }

            if (await ctx.Premises.AnyAsync()) return;

            var premisesList = new List<Premises>
            {
                new() { Name = "The Brazen Head",         Address = "20 Lower Bridge St",      Town = "Dublin",   RiskRating = RiskRating.Low    },
                new() { Name = "Bewley's Oriental Café",  Address = "78 Grafton St",            Town = "Dublin",   RiskRating = RiskRating.Medium },
                new() { Name = "Mulligan's Pub & Kitchen", Address = "8 Poolbeg St",            Town = "Dublin",   RiskRating = RiskRating.High   },
                new() { Name = "The English Market Deli", Address = "Grand Parade",             Town = "Cork",     RiskRating = RiskRating.Low    },
                new() { Name = "Farmgate Café",           Address = "English Market, Princes St",Town = "Cork",     RiskRating = RiskRating.Medium },
                new() { Name = "Liberty Grill",           Address = "32 Washington St",         Town = "Cork",     RiskRating = RiskRating.High   },
                new() { Name = "Bouchon Chez Paul",       Address = "11 Rue Major Martin",      Town = "Lyon",     RiskRating = RiskRating.Medium },
                new() { Name = "Le Café des Fédérations", Address = "8 Rue Major Martin",       Town = "Lyon",     RiskRating = RiskRating.High   },
                new() { Name = "Vieux Lyon Crêperie",     Address = "3 Rue Saint-Jean",         Town = "Lyon",     RiskRating = RiskRating.Low    },
                new() { Name = "Café Louvre",             Address = "Národní 22, Nové Město",   Town = "Prague",   RiskRating = RiskRating.Low    },
                new() { Name = "U Fleku Brewery",         Address = "Kremencova 11",            Town = "Prague",   RiskRating = RiskRating.High   },
                new() { Name = "Lokal Dlouhaaa",          Address = "Dlouha 33, Stare Mesto",   Town = "Prague",   RiskRating = RiskRating.Medium },
            };

            ctx.Premises.AddRange(premisesList);
            await ctx.SaveChangesAsync();

            var today = DateTime.Today;
            var inspections = new List<Inspection>
            {
                new() { PremisesId = premisesList[0].Id,  InspectionDate = today.AddDays(-3),   Score = 92, Outcome = InspectionOutcome.Pass, Notes = "Excellent hygiene throughout. No issues found." },
                new() { PremisesId = premisesList[0].Id,  InspectionDate = today.AddDays(-95),  Score = 74, Outcome = InspectionOutcome.Pass, Notes = "Minor labelling issue on stored goods." },
                new() { PremisesId = premisesList[1].Id,  InspectionDate = today.AddDays(-8),   Score = 48, Outcome = InspectionOutcome.Fail, Notes = "Cold storage temperature above safe threshold." },
                new() { PremisesId = premisesList[1].Id,  InspectionDate = today.AddDays(-55),  Score = 63, Outcome = InspectionOutcome.Pass, Notes = "Improved significantly since previous visit." },
                new() { PremisesId = premisesList[2].Id,  InspectionDate = today.AddDays(-2),   Score = 35, Outcome = InspectionOutcome.Fail, Notes = "Pest evidence found in kitchen area. Immediate action required." },
                new() { PremisesId = premisesList[2].Id,  InspectionDate = today.AddDays(-70),  Score = 58, Outcome = InspectionOutcome.Fail, Notes = "Staff hygiene training not up to date." },
                new() { PremisesId = premisesList[3].Id,  InspectionDate = today.AddDays(-5),   Score = 96, Outcome = InspectionOutcome.Pass, Notes = "Outstanding standards. Best in borough this quarter." },
                new() { PremisesId = premisesList[4].Id,  InspectionDate = today.AddDays(-12),  Score = 81, Outcome = InspectionOutcome.Pass, Notes = "Clean and well organised. Minor ventilation remark." },
                new() { PremisesId = premisesList[4].Id,  InspectionDate = today.AddDays(-60),  Score = 69, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[5].Id,  InspectionDate = today.AddDays(-7),   Score = 42, Outcome = InspectionOutcome.Fail, Notes = "Waste disposal non-compliant. Follow-up required within 14 days." },
                new() { PremisesId = premisesList[5].Id,  InspectionDate = today.AddDays(-110), Score = 77, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[6].Id,  InspectionDate = today.AddDays(-1),   Score = 55, Outcome = InspectionOutcome.Fail, Notes = "Quenelles stored at incorrect temperature." },
                new() { PremisesId = premisesList[6].Id,  InspectionDate = today.AddDays(-45),  Score = 83, Outcome = InspectionOutcome.Pass, Notes = "Good practices observed. Traceability records complete." },
                new() { PremisesId = premisesList[7].Id,  InspectionDate = today.AddDays(-6),   Score = 44, Outcome = InspectionOutcome.Fail, Notes = "Multiple cross-contamination risks identified in prep area." },
                new() { PremisesId = premisesList[7].Id,  InspectionDate = today.AddDays(-80),  Score = 61, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[8].Id,  InspectionDate = today.AddDays(-18),  Score = 88, Outcome = InspectionOutcome.Pass, Notes = "Well managed small kitchen. Staff knowledgeable." },
                new() { PremisesId = premisesList[8].Id,  InspectionDate = today.AddDays(-50),  Score = 72, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[9].Id,  InspectionDate = today.AddDays(-4),   Score = 90, Outcome = InspectionOutcome.Pass, Notes = "Historic venue maintaining excellent modern standards." },
                new() { PremisesId = premisesList[9].Id,  InspectionDate = today.AddDays(-100), Score = 85, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[10].Id, InspectionDate = today.AddDays(-9),   Score = 38, Outcome = InspectionOutcome.Fail, Notes = "Brewery cellar drainage blocked. Serious hygiene concern." },
                new() { PremisesId = premisesList[10].Id, InspectionDate = today.AddDays(-65),  Score = 66, Outcome = InspectionOutcome.Pass, Notes = "Previous issues resolved. Ongoing monitoring recommended." },
                new() { PremisesId = premisesList[11].Id, InspectionDate = today.AddDays(-14),  Score = 79, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[11].Id, InspectionDate = today.AddDays(-30),  Score = 53, Outcome = InspectionOutcome.Fail, Notes = "Glassware sanitisation procedure not followed correctly." },
                new() { PremisesId = premisesList[11].Id, InspectionDate = today.AddDays(-85),  Score = 71, Outcome = InspectionOutcome.Pass },
                new() { PremisesId = premisesList[5].Id,  InspectionDate = today.AddDays(-20),  Score = 47, Outcome = InspectionOutcome.Fail, Notes = "Repeat offender: cold chain management still inadequate." },
            };

            ctx.Inspections.AddRange(inspections);
            await ctx.SaveChangesAsync();

            ctx.FollowUps.AddRange(
                new FollowUp { InspectionId = inspections[2].Id, DueDate = today.AddDays(10), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[4].Id, DueDate = today.AddDays(5), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[9].Id, DueDate = today.AddDays(-4), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[11].Id, DueDate = today.AddDays(-8), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[13].Id, DueDate = today.AddDays(-2), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[19].Id, DueDate = today.AddDays(-15), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[4].Id, DueDate = today.AddDays(-30), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-22) },
                new FollowUp { InspectionId = inspections[9].Id, DueDate = today.AddDays(-25), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-18) },
                new FollowUp { InspectionId = inspections[13].Id, DueDate = today.AddDays(-40), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-35) },
                new FollowUp { InspectionId = inspections[22].Id, DueDate = today.AddDays(-12), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-8) }
            );

            await ctx.SaveChangesAsync();
        }
    }
}