using Portfolio.Domain.Abstracts;

namespace Portfolio.Domain.Entities;

public class ClientCertificate : IEntity
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
}
