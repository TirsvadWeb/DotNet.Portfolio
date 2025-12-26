using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistents;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ClientCertificate> ClientCertificates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /// Configure UserInfo entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            // Optional one-to-one relationship to ClientCertificate
            entity.HasOne(e => e.Certificate)
                .WithOne()
                .HasForeignKey<ApplicationUser>(e => e.CertificateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        /// Configure ClientCertificate entity
        modelBuilder.Entity<ClientCertificate>(entity =>
        {
            entity.ToTable("ClientCertificates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(e => e.Issuer)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(e => e.SerialNumber)
                .IsRequired()
                .HasMaxLength(256);
        });

        //// Conditionally seed development data when running in Development environment
        //string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        //          ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        //if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        //{
        //    Guid certId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");

        //    modelBuilder.Entity<ClientCertificate>().HasData(
        //        new ClientCertificate
        //        {
        //            Id = certId1,
        //            Subject = "CN=TirsvadWebCertDevelopment",
        //            Issuer = "CN=TirsvadWeb",
        //            SerialNumber = "ADMIN-DEV-0001",
        //            ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        //            ValidTo = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        //        }
        //    );

        //    // Provide deterministic/static values for identity fields to avoid EF Core treating the model as changed
        //    // (avoid dynamic values like Guid.NewGuid() or DateTime.Now in HasData)
        //    modelBuilder.Entity<ApplicationUser>().HasData(
        //        new ApplicationUser
        //        {
        //            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        //            Email = "admin@example.local",
        //            EmailConfirmed = true,
        //            LockoutEnabled = false,
        //            CertificateId = certId1,

        //            // Deterministic identity-related fields
        //            AccessFailedCount = 0,
        //            PhoneNumberConfirmed = false,
        //            TwoFactorEnabled = false,
        //            // ConcurrencyStamp must be fixed to avoid non-deterministic migrations
        //            ConcurrencyStamp = "b8d93e9e-edba-4b23-95fc-059696d60e9c",
        //            // Set a normalized email/user name to fixed values
        //            NormalizedEmail = "ADMIN@EXAMPLE.LOCAL",
        //            UserName = "admin@example.local",
        //            NormalizedUserName = "ADMIN@EXAMPLE.LOCAL"
        //        }
        //    );
        //}

    }
}