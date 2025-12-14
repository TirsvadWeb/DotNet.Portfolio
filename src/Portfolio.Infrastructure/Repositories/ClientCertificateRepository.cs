using Microsoft.EntityFrameworkCore;

using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Repositories;

public class ClientCertificateRepository : RepositoryBase<ClientCertificate>, IClientCertificateRepository
{
    private readonly ApplicationDbContext _db;

    public ClientCertificateRepository(ApplicationDbContext db)
        : base(db)
    {
        _db = db;
    }

    public async Task<ClientCertificate?> FindBySubjectAsync(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return null;
        return await _db.ClientCertificates.FirstOrDefaultAsync(c => c.Subject == subject);
    }
}
