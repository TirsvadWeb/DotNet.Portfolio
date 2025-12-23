using Portfolio.Core.Abstracts.Repositories;
using Portfolio.Domain.Entities;

namespace Portfolio.Core.Abstracts;

public interface IClientCertificateRepository : IRepository<ClientCertificate>
{
    Task<ClientCertificate?> FindBySubjectAsync(string subject);
}
