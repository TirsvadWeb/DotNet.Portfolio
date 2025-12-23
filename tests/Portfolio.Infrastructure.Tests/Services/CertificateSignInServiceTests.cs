using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Infrastructure.Tests.Services;

/// <summary>
/// Tests for `CertificateSignInService` covering functional, concurrency and benchmark scenarios.
/// Groups:
/// - Functional: TestCategory("Functional")
/// - Concurrency: TestCategory("Concurrency")
/// - Benchmark: TestCategory("Benchmark") and [DoNotParallelize]
/// </summary>
[TestClass]
public class CertificateSignInServiceTests
{
    private TestClientCertificateRepository _repo = null!;
    private CertificateSignInService? _service;

    [TestInitialize]
    public void Init()
    {
        _repo = new TestClientCertificateRepository();
        _service = new CertificateSignInService(_repo);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task CreatePrincipalForCertificateAsync_SavesNewCertificateAndReturnsPrincipal()
    {
        using X509Certificate2 cert = CreateSelfSigned("CN=func-test");

        ClaimsPrincipal? principal = await _service!.CreatePrincipalForCertificateAsync(cert);

        Assert.IsNotNull(principal, "Expected principal to be created");
        Claim? name = principal!.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.IsNotNull(name);
        Assert.AreEqual(cert.Subject, name!.Value);

        // Repository should have an entry for this subject
        ClientCertificate? stored = await _repo.FindBySubjectAsync(cert.Subject ?? string.Empty);
        Assert.IsNotNull(stored, "Expected repository AddAsync to be invoked and stored entity.");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task CreatePrincipalForCertificateAsync_UsesExistingCertificateWhenPresent()
    {
        using X509Certificate2 cert = CreateSelfSigned("CN=exists-test");

        // Pre-seed repository
        ClientCertificate existing = new()
        {
            Id = Guid.NewGuid(),
            Subject = cert.Subject ?? string.Empty,
            Issuer = cert.Issuer ?? string.Empty,
            SerialNumber = cert.SerialNumber ?? string.Empty,
            ValidFrom = cert.NotBefore,
            ValidTo = cert.NotAfter
        };
        await _repo.AddAsync(existing);

        ClaimsPrincipal? principal = await _service!.CreatePrincipalForCertificateAsync(cert);

        Assert.IsNotNull(principal);
        Assert.AreEqual(1, _repo.AddCallCount, "AddAsync should not be called again for existing certificate (AddCallCount counts unique adds).");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task CreatePrincipalForCertificateAsync_ReturnsNull_WhenRepoThrowsComException()
    {
        // Arrange: repo configured to throw COMException on Find
        _repo.ThrowOnFind = true;
        using X509Certificate2 cert = CreateSelfSigned("CN=com-error");

        ClaimsPrincipal? principal = await _service!.CreatePrincipalForCertificateAsync(cert);

        Assert.IsNull(principal, "Expected null principal when repository throws a COM exception (service swallows exceptions).");
    }

    [TestMethod]
    [TestCategory("Concurrency")]
    public async Task CreatePrincipalForCertificateAsync_ConcurrentCalls_AreThreadSafe()
    {
        using X509Certificate2 cert = CreateSelfSigned("CN=concurrent");

        const int concurrency = 50;
        Task<ClaimsPrincipal?>[] tasks = new Task<ClaimsPrincipal?>[concurrency];

        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = Task.Run(() => _service!.CreatePrincipalForCertificateAsync(cert));
        }

        await Task.WhenAll(tasks);

        ClaimsPrincipal?[] results = [.. tasks.Select(t => t.Result)];
        Assert.IsTrue(results.All(r => r != null), "All concurrent calls should return a principal.");

        // Ensure AddAsync was effectively called only once for new subject
        Assert.AreEqual(1, _repo.AddCallCount, "Repository AddAsync should be called once when concurrent callers race to create the entity.");

        string thumb = results.First(r => r != null)!.Claims.First(c => c.Type == "thumbprint").Value;
        Assert.IsTrue(results.All(r => r!.Claims.First(c => c.Type == "thumbprint").Value == thumb));
    }

    [TestMethod]
    [TestCategory("Benchmark")]
    [DoNotParallelize]
    public async Task CreatePrincipalForCertificateAsync_Performance_ManyCalls_WithinThreshold()
    {
        // Use a repo pre-seeded so service can return quickly and test hot-path behavior
        using X509Certificate2 cert = CreateSelfSigned("CN=perf");
        await _repo.FindBySubjectAsync(cert.Subject ?? string.Empty); // ensure repo is reachable

        // Pre-seed the repo by invoking service once
        _ = await _service!.CreatePrincipalForCertificateAsync(cert);

        const int calls = 500;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < calls; i++)
        {
            _ = await _service.CreatePrincipalForCertificateAsync(cert);
        }
        sw.Stop();

        // Conservative threshold: 5 seconds for 500 calls on CI can be adjusted
        Assert.IsLessThan(5000, sw.ElapsedMilliseconds, $"Expected {calls} calls to complete under 5000ms, took {sw.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task CreatePrincipalForCertificateAsync_MultiSession_AddRemoveRepeat_BehavesCorrectly()
    {
        using X509Certificate2 cert = CreateSelfSigned("CN=multisession");

        // Iterate: add via service then remove from repo and ensure subsequent call returns null
        ClaimsPrincipal? first = await _service!.CreatePrincipalForCertificateAsync(cert);
        Assert.IsNotNull(first);

        // Remove from repo to simulate session end / cleanup
        await _repo.DeleteBySubjectAsync(cert.Subject ?? string.Empty);

        ClaimsPrincipal? second = await _service.CreatePrincipalForCertificateAsync(cert);
        Assert.IsNotNull(second, "Service should recreate the stored entity when missing (it persists on first use).");
    }

    // Helper: create a self-signed cert in-memory
    private static X509Certificate2 CreateSelfSigned(string subject)
    {
        using RSA rsa = System.Security.Cryptography.RSA.Create(2048);
        CertificateRequest req = new(subject, rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 cert = req.CreateSelfSigned(now.AddDays(-1), now.AddYears(1));
        // Use a direct import of the exported PFX bytes to avoid platform-specific loader issues in tests
        X509Certificate2 rs1 = X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), "");
        return rs1;
    }

    // Minimal thread-safe test double for IClientCertificateRepository
    private sealed class TestClientCertificateRepository : IClientCertificateRepository
    {
        private readonly ConcurrentDictionary<string, ClientCertificate> _store = new(StringComparer.OrdinalIgnoreCase);
        public bool ThrowOnFind { get; set; }
        private int _addCallCount;
        public int AddCallCount => _addCallCount;

        public Task AddAsync(ClientCertificate entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            // store by subject
            _store[entity.Subject] = entity;
            Interlocked.Increment(ref _addCallCount);
            return Task.CompletedTask;
        }

        // Provide a helper to delete by subject for multi-session test
        public Task DeleteBySubjectAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return Task.CompletedTask;
            _store.TryRemove(subject, out _);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ClientCertificate entity)
        {
            if (entity == null) return Task.CompletedTask;
            _store.TryRemove(entity.Subject, out _);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ClientCertificate>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<ClientCertificate>>([.. _store.Values]);
        }

        public Task<ClientCertificate?> GetByIdAsync(Guid id)
        {
            ClientCertificate? found = _store.Values.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(found);
        }

        public Task<ClientCertificate?> FindBySubjectAsync(string subject)
        {
            if (ThrowOnFind)
            {
                // simulate COM error or other native interop failure
                throw new COMException("Simulated COM failure");
            }

            if (string.IsNullOrWhiteSpace(subject)) return Task.FromResult<ClientCertificate?>(null);
            if (_store.TryGetValue(subject, out ClientCertificate? found))
            {
                return Task.FromResult<ClientCertificate?>(found);
            }
            return Task.FromResult<ClientCertificate?>(null);
        }

        public Task UpdateAsync(ClientCertificate entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _store[entity.Subject] = entity;
            return Task.CompletedTask;
        }
    }
}
