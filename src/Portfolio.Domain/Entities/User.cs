using Portfolio.Domain.Abstracts;

namespace Portfolio.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; set; } = Guid.Empty;
    public Guid? CertificateId { get; set; } = null;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    virtual public ClientCertificate? Certificate { get; set; }
}
