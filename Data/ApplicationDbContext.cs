using Microsoft.EntityFrameworkCore;
using RegistrationApp.Models;

namespace RegistrationApp.Data;

/// <summary>
/// Entity Framework Core DbContext for the registration application
/// Manages all database operations for Registrations, Payments, and Categories
/// </summary>
public class ApplicationDbContext : DbContext
{
    // Fixed, deterministic IST seed timestamp (avoids new model diffs on every scaffold).
    private static readonly DateTime SeedCreatedAt = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Category> Categories { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress warning for dynamic seed data - timestamps are intentionally dynamic
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("INR");

            entity.Property(c => c.RegistrationFee)
                .HasPrecision(18, 2);

            entity.HasIndex(c => c.Name)
                .IsUnique();

            // Seed default categories
            entity.HasData(
                new Category
                {
                    Id = 1,
                    Name = "Batsman",
                    Description = "Batsman category",
                    RegistrationFee = 500, // Rs 500
                    Currency = "INR",
                    CreatedAt = SeedCreatedAt
                },
                new Category
                {
                    Id = 2,
                    Name = "Bowler",
                    Description = "Bowler category",
                    RegistrationFee = 500, // Rs 500
                    Currency = "INR",
                    CreatedAt = SeedCreatedAt
                },
                new Category
                {
                    Id = 3,
                    Name = "All Rounder",
                    Description = "All Rounder category",
                    RegistrationFee = 500, // Rs 500
                    Currency = "INR",
                    CreatedAt = SeedCreatedAt
                }
            );
        });

        // Configure Registration
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(r => r.Address)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(r => r.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(r => r.Taluka)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(r => r.TShirtSize)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(r => r.PhotoBlobName)
                .HasMaxLength(500);

            entity.Property(r => r.AadharFrontBlobName)
                .HasMaxLength(500);

            entity.Property(r => r.AadharBackBlobName)
                .HasMaxLength(500);

            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(RegistrationStatus.PaymentPending);

            entity.Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(r => r.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationship to Category
            entity.HasOne(r => r.Category)
                .WithMany(c => c.Registrations)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-many relationship with Payment
            entity.HasMany(r => r.Payments)
                .WithOne(p => p.Registration)
                .HasForeignKey(p => p.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(r => r.PhoneNumber);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);
            entity.HasIndex(r => new { r.PhoneNumber, r.Status });
        });

        // Configure Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.RazorpayOrderId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.RazorpayPaymentId)
                .HasMaxLength(100);

            entity.Property(p => p.AmountInPaise)
                .HasPrecision(18, 2);

            entity.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("INR");

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(PaymentStatus.Created);

            entity.Property(p => p.ErrorMessage)
                .HasMaxLength(1000);

            entity.Property(p => p.RazorpaySignature)
                .HasMaxLength(500);

            entity.Property(p => p.IdempotencyKey)
                .HasMaxLength(100);

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Foreign key is configured in Registration

            // Indexes for common queries
            entity.HasIndex(p => p.RazorpayOrderId)
                .IsUnique();

            entity.HasIndex(p => p.RazorpayPaymentId);

            entity.HasIndex(p => p.RegistrationId);

            entity.HasIndex(p => p.Status);

            entity.HasIndex(p => p.IdempotencyKey);

            entity.HasIndex(p => new { p.RegistrationId, p.Status });
        });
    }
}
