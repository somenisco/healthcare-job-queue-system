using JobProcessor.Core;
using Microsoft.EntityFrameworkCore;

namespace JobProcessor.Infrastructure;

public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options)
        : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Sample> Samples => Set<Sample>();
    public DbSet<TestOrder> TestOrders => Set<TestOrder>();
    public DbSet<TestResult> TestResults => Set<TestResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>()
            .Property(p => p.PatientId)
            .ValueGeneratedNever();

        modelBuilder.Entity<Sample>()
            .Property(s => s.SampleId)
            .ValueGeneratedNever();

        modelBuilder.Entity<TestOrder>()
            .Property(o => o.TestOrderId)
            .ValueGeneratedNever();

        modelBuilder.Entity<TestResult>()
            .Property(r => r.TestResultId)
            .ValueGeneratedNever();

        modelBuilder.Entity<TestOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();

        modelBuilder.Entity<TestOrder>()
            .HasIndex(o => new { o.Status, o.UpdatedAt, o.TestOrderId })
            .HasDatabaseName("IX_TestOrders_Status_UpdatedAt_TestOrderId");

        modelBuilder.Entity<TestOrder>()
            .HasIndex(o => new { o.UpdatedAt, o.TestOrderId })
            .HasDatabaseName("IX_TestOrders_UpdatedAt_TestOrderId");

        modelBuilder.Entity<TestOrder>()
            .HasOne(o => o.TestResult)
            .WithOne(r => r.TestOrder)
            .HasForeignKey<TestResult>(r => r.TestOrderId);

        modelBuilder.Entity<TestOrder>()
            .HasOne(o => o.Sample)
            .WithMany(s => s.TestOrders)
            .HasForeignKey(o => o.SampleId);

        modelBuilder.Entity<Sample>()
            .HasOne(s => s.Patient)
            .WithMany(p => p.Samples)
            .HasForeignKey(s => s.PatientId);
    }
}
