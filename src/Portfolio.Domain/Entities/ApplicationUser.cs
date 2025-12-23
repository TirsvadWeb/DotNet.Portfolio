using Microsoft.AspNetCore.Identity;

using Portfolio.Domain.Abstracts;

namespace Portfolio.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IEntity
{
    [PersonalData]
    public override Guid Id { get; set; } = Guid.Empty;
    public Guid? CertificateId { get; set; } = null;
    public virtual ClientCertificate? Certificate { get; set; }

    // Added to match EF Core migration snapshot which expects an `IsActive` column
    // Default value true matches the migration's HasDefaultValue(true)
    public bool IsActive { get; set; } = true;
}
