using Microsoft.EntityFrameworkCore;

using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistents;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClientCertificate> ClientCertificates { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /// Configure UserInfo entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Optional one-to-one relationship to ClientCertificate
            entity.HasOne(e => e.Certificate)
                .WithOne()
                .HasForeignKey<User>(e => e.CertificateId)
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
    }
}