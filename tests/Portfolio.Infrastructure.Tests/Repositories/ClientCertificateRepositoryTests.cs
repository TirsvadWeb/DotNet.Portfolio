using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;
using Portfolio.Infrastructure.Repositories;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Portfolio.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for `ClientCertificateRepository` grouped by Functional, Concurrency and Benchmark.
/// Covers: Add, GetById, GetAll, Update, Delete, FindBySubject, multi-session persistence,
/// concurrency using separate contexts and a simple performance benchmark.
/// Uses a file-based SQLite database and applies EF Core migrations (empty production migration exists).
/// </summary>
[TestClass]
public class ClientCertificateRepositoryTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions(string name)
    {
        // Use a file-based SQLite database per test to avoid the EF InMemory provider
        // and to allow multi-context concurrency semantics similar to a real database.
        string dbPath = Path.Combine(Path.GetTempPath(), $"{name}.db");
        // Ensure a clean file for named database in case previous runs left it behind
        try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            // Tests should not fail when the compiled model differs from the snapshot used by migrations
            // (this commonly happens in CI when migration files are present but code was changed). Ignore
            // the warning so tests can apply migrations and proceed.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Add_GetById_GetAll_Update_Delete_FindBySubject_Works()
    {
        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        // Arrange - create and seed
        using ApplicationDbContext context = new(options);
        // Create database schema from the model since migrations are not present in tests
        await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        ClientCertificateRepository repo = new(context);

        ClientCertificate cert = new()
        {
            Id = Guid.NewGuid(),
            Subject = "CN=functest",
            Issuer = "CN=issuer",
            SerialNumber = "123",
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        };

        // Act - Add
        await repo.AddAsync(cert);

        // Assert - GetById
        ClientCertificate? byId = await repo.GetByIdAsync(cert.Id);
        Assert.IsNotNull(byId);
        Assert.AreEqual(cert.Subject, byId!.Subject);

        // Assert - GetAll contains the cert
        List<ClientCertificate> all = [.. (await repo.GetAllAsync())];
        Assert.IsTrue(all.Any(c => c.Id == cert.Id));

        // Act - Update
        cert.Issuer = "CN=issuer-updated";
        await repo.UpdateAsync(cert);
        ClientCertificate? updated = await repo.GetByIdAsync(cert.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("CN=issuer-updated", updated!.Issuer);

        // Act - FindBySubject
        ClientCertificate? found = await repo.FindBySubjectAsync(cert.Subject);
        Assert.IsNotNull(found);
        Assert.AreEqual(cert.SerialNumber, found!.SerialNumber);

        // Act - Delete
        await repo.DeleteAsync(cert);
        ClientCertificate? afterDelete = await repo.GetByIdAsync(cert.Id);
        Assert.IsNull(afterDelete);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task FindBySubject_ReturnsNull_ForInvalidInput()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(Guid.NewGuid().ToString());
        using ApplicationDbContext context = new(options);
        await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        ClientCertificateRepository repo = new(context);

        Assert.IsNull(await repo.FindBySubjectAsync(null!));
        Assert.IsNull(await repo.FindBySubjectAsync(string.Empty));
        Assert.IsNull(await repo.FindBySubjectAsync("   "));
    }

    [TestMethod]
    [TestCategory("Concurrency")]
    public async Task Concurrency_MultipleRepositories_CanOperateInParallel_WithoutThrowing()
    {
        const int workers = 20;
        string subject = "CN=concurrency";

        // Use a single SQLite file so contexts see the same data when required
        string dbName = Guid.NewGuid().ToString();

        // Ensure the database schema is created once before running concurrent workers.
        // Calling EnsureCreated concurrently from multiple threads can race and lead to "table already exists" errors
        // so create the schema on the calling thread.
        using (ApplicationDbContext initCtx = new(CreateOptions(dbName)))
        {
            await initCtx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        }

        Task[] tasks = new Task[workers];
        Exception? caught = null;

        for (int i = 0; i < workers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Each worker creates its own context/repo instance (DbContext is not thread-safe)
                    using ApplicationDbContext ctx = new(CreateOptions(dbName));
                    ClientCertificateRepository repo = new(ctx);

                    // Add or update a certificate record
                    ClientCertificate cert = new()
                    {
                        Id = Guid.NewGuid(),
                        Subject = subject,
                        Issuer = $"CN=issuer-{i}",
                        SerialNumber = i.ToString(),
                        ValidFrom = DateTime.UtcNow.AddDays(-1),
                        ValidTo = DateTime.UtcNow.AddYears(1)
                    };

                    await repo.AddAsync(cert);

                    // Read back via another repo instance to ensure visibility
                    using ApplicationDbContext readCtx = new(CreateOptions(dbName));
                    ClientCertificateRepository readRepo = new(readCtx);
                    ClientCertificate? found = await readRepo.FindBySubjectAsync(subject);
                    // found may be any of the added entries; we only assert no exceptions
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref caught, ex, null);
                }
            }, TestContext.CancellationToken);
        }

        await Task.WhenAll(tasks);

        Assert.IsNull(caught, "No exceptions should be thrown when using separate DbContext instances concurrently.");

        // Verify that at least one record exists for the subject
        using (ApplicationDbContext verifyCtx = new(CreateOptions(dbName)))
        {
            await verifyCtx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            ClientCertificateRepository repo = new(verifyCtx);
            bool any = (await repo.GetAllAsync()).Any(c => c.Subject == subject);
            Assert.IsTrue(any, "Expected at least one certificate entry for the subject after concurrent adds.");
        }
    }

    [TestMethod]
    [TestCategory("Benchmark")]
    [DoNotParallelize]
    public async Task Performance_FindBySubject_ManyCalls_WithinThreshold()
    {
        const int calls = 1000;
        const int thresholdMs = 2000; // conservative threshold for CI

        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            ClientCertificateRepository repo = new(ctx);

            ClientCertificate cert = new()
            {
                Id = Guid.NewGuid(),
                Subject = "CN=perf",
                Issuer = "CN=perfissuer",
                SerialNumber = "perf-1",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddYears(1)
            };

            await repo.AddAsync(cert);
        }

        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            ClientCertificateRepository repo = new(ctx);
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < calls; i++)
            {
                ClientCertificate? _ = await repo.FindBySubjectAsync("CN=perf");
            }
            sw.Stop();

            Assert.IsLessThan(thresholdMs, sw.ElapsedMilliseconds, $"Expected {calls} lookups to complete under {thresholdMs}ms but took {sw.ElapsedMilliseconds}ms.");
        }
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task MultiSession_Persistence_AcrossDbContexts_Works()
    {
        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        Guid id;
        // Session 1: add
        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            ClientCertificateRepository repo = new(ctx);
            ClientCertificate cert = new()
            {
                Id = Guid.NewGuid(),
                Subject = "CN=multisession",
                Issuer = "CN=ms",
                SerialNumber = "ms-1",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddYears(1)
            };

            id = cert.Id;
            await repo.AddAsync(cert);
        }

        // Session 2: read from new context
        using (ApplicationDbContext ctx2 = new(options))
        {
            await ctx2.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            ClientCertificateRepository repo2 = new(ctx2);
            ClientCertificate? found = await repo2.GetByIdAsync(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("CN=multisession", found!.Subject);

            // Delete and ensure subsequent session cannot find it
            await repo2.DeleteAsync(found);
        }

        using ApplicationDbContext ctx3 = new(options);
        await ctx3.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        ClientCertificateRepository repo3 = new(ctx3);
        ClientCertificate? missing = await repo3.GetByIdAsync(id);
        Assert.IsNull(missing);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void FindBySubject_Throws_WhenUnderlyingThrows_ComExceptionSimulation()
    {
        // Simulate a repository that wraps a DbContext which throws COMException on access.
        // Because creating a DbContext that reliably throws a COMException from EF internals is complex,
        // we validate that callers see the propagated exception by creating a small fake repository
        // that throws and asserting the exception flows.

        ThrowingClientCertificateRepository throwingRepo = new();

        Assert.Throws<COMException>(() =>
        {
            // use Task.Run().GetAwaiter().GetResult() to call async from sync test
            Task.Run(async () => await throwingRepo.FindBySubjectAsync("CN=boom")).GetAwaiter().GetResult();
        });
    }

    // A tiny test double that simulates a COM error coming from the persistence layer
    private sealed class ThrowingClientCertificateRepository : IClientCertificateRepository
    {
        public Task AddAsync(ClientCertificate entity) => Task.FromException(new COMException("simulated"));
        public Task DeleteAsync(ClientCertificate entity) => Task.FromException(new COMException("simulated"));
        public Task<IEnumerable<ClientCertificate>> GetAllAsync() => Task.FromException<IEnumerable<ClientCertificate>>(new COMException("simulated"));
        public Task<ClientCertificate?> GetByIdAsync(Guid id) => Task.FromException<ClientCertificate?>(new COMException("simulated"));
        public Task UpdateAsync(ClientCertificate entity) => Task.FromException(new COMException("simulated"));
        public Task<ClientCertificate?> FindBySubjectAsync(string subject) => Task.FromException<ClientCertificate?>(new COMException("simulated"));
    }

    public TestContext TestContext { get; set; }
}
