namespace Portfolio.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.Empty;
    public Guid CertificateId { get; set; } = Guid.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    virtual public ClientCertificate? Certificate { get; set; }
}
