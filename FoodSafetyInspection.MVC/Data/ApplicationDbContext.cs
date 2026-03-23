using FoodSafetyInspection.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyInspection.MVC.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Premises> Premises => Set<Premises>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Inspection>()
            .HasOne(i => i.Premises).WithMany(p => p.Inspections)
            .HasForeignKey(i => i.PremisesId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FollowUp>()
            .HasOne(f => f.Inspection).WithMany(i => i.FollowUps)
            .HasForeignKey(f => f.InspectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
