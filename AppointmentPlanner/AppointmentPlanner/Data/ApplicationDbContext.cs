using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppointmentPlanner.Models.Identity;
using AppointmentPlanner.Models;

namespace AppointmentPlanner.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Existing appointment system tables
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<WaitingList> WaitingLists { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<WorkDay> WorkDays { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProfilePicture).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // Configure existing appointment system entities
        ConfigureAppointmentEntities(builder);

        // Seed default roles - commented out for initial migration
        // SeedRoles(builder);
    }

    private void ConfigureAppointmentEntities(ModelBuilder builder)
    {
        builder.Entity<Hospital>(entity =>
        {
            entity.ToTable("Hospitals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Disease).HasMaxLength(200);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Symptoms).HasMaxLength(500);
            entity.Property(e => e.RecurrenceRule).HasMaxLength(1000);
            entity.Property(e => e.RecurrenceException).HasMaxLength(1000);
            entity.Property(e => e.StartTimezone).HasMaxLength(100);
            entity.Property(e => e.EndTimezone).HasMaxLength(100);
            entity.Property(e => e.ElementType).HasMaxLength(50);
        });

        builder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Text).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Education).HasMaxLength(200);
            entity.Property(e => e.Specialization).HasMaxLength(100);
            entity.Property(e => e.Experience).HasMaxLength(50);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.DutyTiming).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Mobile).HasMaxLength(20);
            entity.Property(e => e.Availability).HasMaxLength(20);
            entity.Property(e => e.StartHour).HasMaxLength(10);
            entity.Property(e => e.EndHour).HasMaxLength(10);
        });

        builder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Text).HasMaxLength(100);
            entity.Property(e => e.Mobile).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Disease).HasMaxLength(200);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.BloodGroup).HasMaxLength(10);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Symptoms).HasMaxLength(500);
        });

        builder.Entity<WaitingList>(entity =>
        {
            entity.ToTable("WaitingLists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Disease).HasMaxLength(200);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Treatment).HasMaxLength(100);
        });

        builder.Entity<Specialization>(entity =>
        {
            entity.ToTable("Specializations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(20);
        });

        builder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities");
            entity.HasKey(e => e.Name);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Time).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        builder.Entity<WorkDay>(entity =>
        {
            entity.ToTable("WorkDays");
            entity.HasKey(e => new { e.Day, e.Index });
            entity.Property(e => e.Day).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Index).IsRequired();
        });
    }


}
