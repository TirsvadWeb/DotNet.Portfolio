using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;

using System.Diagnostics;

namespace Portfolio;

/// <summary>
/// Development-only data seeder.
/// Keeps Program.cs minimal by encapsulating development seed logic here.
/// </summary>
internal static class DevelopmentDataSeeder
{
    public static void Seed(ApplicationDbContext db, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment()) return;

        const string seedEmail = "john.doe@example.com";
        if (db.Users.Any(u => u.Email == seedEmail)) return;

        ClientCertificate cert = new()
        {
            Id = Guid.NewGuid(),
            Subject = "CN=john Doe",
            Issuer = "CN=TirsvadWebCertDevelopment",
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1),
            SerialNumber = Guid.NewGuid().ToString("N").ToUpperInvariant()
        };

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            Email = seedEmail,
            CertificateId = cert.Id,
            Certificate = cert
        };

        db.ClientCertificates.Add(cert);
        db.Users.Add(user);
        db.SaveChanges();

        Debug.WriteLine($"Seeded development user '{seedEmail}' with certificate '{cert.Subject}'.");
    }
}
