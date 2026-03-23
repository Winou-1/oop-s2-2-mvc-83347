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

            // --- Roles ---
            foreach (var role in new[] { "Admin", "Inspector", "Viewer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // --- Admin user ---
            const string adminEmail = "admin@council.ie";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin@1234");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // --- Inspector user ---
            const string inspectorEmail = "inspector@council.ie";
            if (await userManager.FindByEmailAsync(inspectorEmail) is null)
            {
                var inspector = new IdentityUser { UserName = inspectorEmail, Email = inspectorEmail, EmailConfirmed = true };
                await userManager.CreateAsync(inspector, "Inspector@1234");
                await userManager.AddToRoleAsync(inspector, "Inspector");
            }

            // --- Viewer user ---
            const string viewerEmail = "viewer@council.ie";
            if (await userManager.FindByEmailAsync(viewerEmail) is null)
            {
                var viewer = new IdentityUser { UserName = viewerEmail, Email = viewerEmail, EmailConfirmed = true };
                await userManager.CreateAsync(viewer, "Viewer@1234");
                await userManager.AddToRoleAsync(viewer, "Viewer");
            }

            if (await ctx.Premises.AnyAsync()) return;

            // --- Premises (12 across 3 towns) ---
            var towns = new[] { "Cork", "Dublin", "Galway" };
            var risks = new[] { RiskRating.Low, RiskRating.Medium, RiskRating.High };

            var premisesList = new List<Premises>
        {
            new() { Name = "The Spice Kitchen",   Address = "12 Main St",    Town = "Cork",   RiskRating = RiskRating.High   },
            new() { Name = "Cork Burger Co",       Address = "3 Bridge Rd",   Town = "Cork",   RiskRating = RiskRating.Medium },
            new() { Name = "Harbour Fish & Chips", Address = "7 Dock Lane",   Town = "Cork",   RiskRating = RiskRating.Low    },
            new() { Name = "Atlantic Bakery",      Address = "22 Sea View",   Town = "Cork",   RiskRating = RiskRating.Medium },
            new() { Name = "Liffey Bistro",        Address = "45 Quay St",    Town = "Dublin", RiskRating = RiskRating.High   },
            new() { Name = "Temple Bar Grill",     Address = "8 Temple Lane", Town = "Dublin", RiskRating = RiskRating.High   },
            new() { Name = "Dublin Deli",          Address = "99 O'Connell",  Town = "Dublin", RiskRating = RiskRating.Low    },
            new() { Name = "Capital Sushi",        Address = "14 Dame St",    Town = "Dublin", RiskRating = RiskRating.Medium },
            new() { Name = "Galway Bay Seafood",   Address = "2 Dock Rd",     Town = "Galway", RiskRating = RiskRating.High   },
            new() { Name = "West End Café",        Address = "55 Shop St",    Town = "Galway", RiskRating = RiskRating.Low    },
            new() { Name = "Connemara Steakhouse", Address = "7 Market St",   Town = "Galway", RiskRating = RiskRating.Medium },
            new() { Name = "Salthill Chipper",     Address = "1 Prom Walk",   Town = "Galway", RiskRating = RiskRating.Low    },
        };

            ctx.Premises.AddRange(premisesList);
            await ctx.SaveChangesAsync();

            // --- 25 Inspections ---
            var today = DateTime.Today;
            var inspections = new List<Inspection>
        {
            new() { PremisesId=premisesList[0].Id,  InspectionDate=today.AddDays(-2),   Score=45, Outcome=InspectionOutcome.Fail, Notes="Multiple hygiene violations." },
            new() { PremisesId=premisesList[0].Id,  InspectionDate=today.AddDays(-40),  Score=72, Outcome=InspectionOutcome.Pass, Notes="Improved since last visit." },
            new() { PremisesId=premisesList[1].Id,  InspectionDate=today.AddDays(-5),   Score=88, Outcome=InspectionOutcome.Pass, Notes="Clean and well managed." },
            new() { PremisesId=premisesList[2].Id,  InspectionDate=today.AddDays(-10),  Score=55, Outcome=InspectionOutcome.Fail, Notes="Cold storage temperature issue." },
            new() { PremisesId=premisesList[3].Id,  InspectionDate=today.AddDays(-15),  Score=91, Outcome=InspectionOutcome.Pass, Notes="Excellent standards." },
            new() { PremisesId=premisesList[4].Id,  InspectionDate=today.AddDays(-3),   Score=40, Outcome=InspectionOutcome.Fail, Notes="Pest control required." },
            new() { PremisesId=premisesList[4].Id,  InspectionDate=today.AddDays(-60),  Score=65, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[5].Id,  InspectionDate=today.AddDays(-7),   Score=78, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[6].Id,  InspectionDate=today.AddDays(-12),  Score=95, Outcome=InspectionOutcome.Pass, Notes="Outstanding." },
            new() { PremisesId=premisesList[7].Id,  InspectionDate=today.AddDays(-20),  Score=50, Outcome=InspectionOutcome.Fail, Notes="Staff training needed." },
            new() { PremisesId=premisesList[8].Id,  InspectionDate=today.AddDays(-1),   Score=62, Outcome=InspectionOutcome.Fail, Notes="Waste disposal non-compliant." },
            new() { PremisesId=premisesList[9].Id,  InspectionDate=today.AddDays(-8),   Score=84, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[10].Id, InspectionDate=today.AddDays(-25),  Score=77, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[11].Id, InspectionDate=today.AddDays(-6),   Score=48, Outcome=InspectionOutcome.Fail },
            new() { PremisesId=premisesList[0].Id,  InspectionDate=today.AddDays(-90),  Score=58, Outcome=InspectionOutcome.Fail },
            new() { PremisesId=premisesList[1].Id,  InspectionDate=today.AddDays(-100), Score=80, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[2].Id,  InspectionDate=today.AddDays(-45),  Score=70, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[3].Id,  InspectionDate=today.AddDays(-55),  Score=60, Outcome=InspectionOutcome.Fail },
            new() { PremisesId=premisesList[5].Id,  InspectionDate=today.AddDays(-4),   Score=85, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[6].Id,  InspectionDate=today.AddDays(-9),   Score=92, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[7].Id,  InspectionDate=today.AddDays(-14),  Score=44, Outcome=InspectionOutcome.Fail },
            new() { PremisesId=premisesList[8].Id,  InspectionDate=today.AddDays(-30),  Score=73, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[9].Id,  InspectionDate=today.AddDays(-35),  Score=89, Outcome=InspectionOutcome.Pass },
            new() { PremisesId=premisesList[10].Id, InspectionDate=today.AddDays(-50),  Score=55, Outcome=InspectionOutcome.Fail },
            new() { PremisesId=premisesList[11].Id, InspectionDate=today.AddDays(-70),  Score=66, Outcome=InspectionOutcome.Pass },
        };

            ctx.Inspections.AddRange(inspections);
            await ctx.SaveChangesAsync();

            // --- 10 FollowUps (mix of overdue/closed) ---
            ctx.FollowUps.AddRange(
                new FollowUp { InspectionId = inspections[0].Id, DueDate = today.AddDays(7), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[0].Id, DueDate = today.AddDays(-5), Status = FollowUpStatus.Open },  // overdue
                new FollowUp { InspectionId = inspections[3].Id, DueDate = today.AddDays(-10), Status = FollowUpStatus.Open },  // overdue
                new FollowUp { InspectionId = inspections[5].Id, DueDate = today.AddDays(-3), Status = FollowUpStatus.Open },  // overdue
                new FollowUp { InspectionId = inspections[9].Id, DueDate = today.AddDays(14), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[10].Id, DueDate = today.AddDays(-7), Status = FollowUpStatus.Open },  // overdue
                new FollowUp { InspectionId = inspections[13].Id, DueDate = today.AddDays(3), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspections[3].Id, DueDate = today.AddDays(-20), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-15) },
                new FollowUp { InspectionId = inspections[5].Id, DueDate = today.AddDays(-30), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-25) },
                new FollowUp { InspectionId = inspections[9].Id, DueDate = today.AddDays(-15), Status = FollowUpStatus.Closed, ClosedDate = today.AddDays(-10) }
            );

            await ctx.SaveChangesAsync();
        }
    }
}
