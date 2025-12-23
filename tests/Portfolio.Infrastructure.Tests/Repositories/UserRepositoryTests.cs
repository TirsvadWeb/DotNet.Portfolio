using Microsoft.EntityFrameworkCore;

using Portfolio.Core.Abstracts.Repositories;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;
using Portfolio.Infrastructure.Repositories;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Portfolio.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for `UserRepository` grouped by Functional, Concurrency and Benchmark.
/// Exercises: Add, GetById, GetAll, Update, Delete, multi-session persistence,
/// concurrency using separate contexts and a simple performance benchmark.
/// Uses a file-based SQLite database and applies EF Core migrations.
/// </summary>
[TestClass]
public class UserRepositoryTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions(string name)
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"{name}.db");
        try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Add_GetById_GetAll_Update_Delete_Works()
    {
        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        using ApplicationDbContext context = new(options);
        await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        UserRepository repo = new(context);

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            Email = $"user+{Guid.NewGuid():N}@example.test",
            CertificateId = null
        };

        await repo.AddAsync(user);

        ApplicationUser? byId = await repo.GetByIdAsync(user.Id);
        Assert.IsNotNull(byId);
        Assert.AreEqual(user.Email, byId!.Email);

        List<ApplicationUser> all = [.. (await repo.GetAllAsync())];
        Assert.IsTrue(all.Any(u => u.Id == user.Id));

        // Update
        await repo.UpdateAsync(user);
        ApplicationUser? updated = await repo.GetByIdAsync(user.Id);
        Assert.IsNotNull(updated);

        // Delete
        await repo.DeleteAsync(user);
        ApplicationUser? afterDelete = await repo.GetByIdAsync(user.Id);
        Assert.IsNull(afterDelete);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetById_ReturnsNull_ForMissingId()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(Guid.NewGuid().ToString());
        using ApplicationDbContext context = new(options);
        await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        UserRepository repo = new(context);

        ApplicationUser? missing = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.IsNull(missing);
    }

    [TestMethod]
    [TestCategory("Concurrency")]
    public async Task Concurrency_MultipleRepositories_CanOperateInParallel_WithoutThrowing()
    {
        const int workers = 20;
        string dbName = Guid.NewGuid().ToString();

        // Ensure the database schema is created once before running concurrent workers.
        using (ApplicationDbContext initCtx = new(CreateOptions(dbName)))
        {
            await initCtx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        }

        Task[] tasks = new Task[workers];
        Exception? caught = null;

        for (int i = 0; i < workers; i++)
        {
            int idx = i;
            tasks[idx] = Task.Run(async () =>
            {
                try
                {
                    using ApplicationDbContext ctx = new(CreateOptions(dbName));
                    UserRepository repo = new(ctx);

                    ApplicationUser user = new()
                    {
                        Id = Guid.NewGuid(),
                        Email = $"concurrent{idx}+{Guid.NewGuid():N}@example.test",
                    };

                    await repo.AddAsync(user);

                    using ApplicationDbContext readCtx = new(CreateOptions(dbName));
                    UserRepository readRepo = new(readCtx);
                    ApplicationUser? found = await readRepo.GetByIdAsync(user.Id);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref caught, ex, null);
                }
            }, TestContext.CancellationToken);
        }

        await Task.WhenAll(tasks);

        Assert.IsNull(caught, "No exceptions should be thrown when using separate DbContext instances concurrently.");

        // Verify some users were added
        using (ApplicationDbContext verifyCtx = new(CreateOptions(dbName)))
        {
            await verifyCtx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            UserRepository repo = new(verifyCtx);
            bool any = (await repo.GetAllAsync()).Any();
            Assert.IsTrue(any, "Expected at least one user after concurrent adds.");
        }
    }

    [TestMethod]
    [TestCategory("Benchmark")]
    [DoNotParallelize]
    public async Task Performance_GetById_ManyCalls_WithinThreshold()
    {
        const int calls = 1000;
        const int thresholdMs = 2000;

        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        Guid id;
        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            UserRepository repo = new(ctx);

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                Email = $"perf+{Guid.NewGuid():N}@example.test",
            };

            id = user.Id;
            await repo.AddAsync(user);
        }

        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            UserRepository repo = new(ctx);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < calls; i++)
            {
                ApplicationUser? _ = await repo.GetByIdAsync(id);
            }
            sw.Stop();

            Assert.IsLessThan(thresholdMs,
                sw.ElapsedMilliseconds, $"Expected {calls} lookups to complete under {thresholdMs}ms but took {sw.ElapsedMilliseconds}ms.");
        }
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task MultiSession_Persistence_AcrossDbContexts_Works()
    {
        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);

        Guid id;
        using (ApplicationDbContext ctx = new(options))
        {
            await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            UserRepository repo = new(ctx);
            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                Email = $"ms+{Guid.NewGuid():N}@example.test"
            };

            id = user.Id;
            await repo.AddAsync(user);
        }

        using (ApplicationDbContext ctx2 = new(options))
        {
            await ctx2.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            UserRepository repo2 = new(ctx2);
            ApplicationUser? found = await repo2.GetByIdAsync(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("ms", found!.Email!.Split('@')[0][..2]);

            await repo2.DeleteAsync(found);
        }

        using ApplicationDbContext ctx3 = new(options);
        await ctx3.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        UserRepository repo3 = new(ctx3);
        ApplicationUser? missing = await repo3.GetByIdAsync(id);
        Assert.IsNull(missing);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void FindById_Throws_WhenUnderlyingThrows_ComExceptionSimulation()
    {
        ThrowingUserRepository throwingRepo = new();

        Assert.Throws<COMException>(() =>
        {
            Task.Run(async () => await throwingRepo.GetByIdAsync(Guid.NewGuid())).GetAwaiter().GetResult();
        });
    }

    // A tiny test double that simulates a COM error coming from the persistence layer
    private sealed class ThrowingUserRepository : IRepository<ApplicationUser>
    {
        public Task AddAsync(ApplicationUser entity) => Task.FromException(new COMException("simulated"));
        public Task DeleteAsync(ApplicationUser entity) => Task.FromException(new COMException("simulated"));
        public Task<IEnumerable<ApplicationUser>> GetAllAsync() => Task.FromException<IEnumerable<ApplicationUser>>(new COMException("simulated"));
        public Task<ApplicationUser?> GetByIdAsync(Guid id) => Task.FromException<ApplicationUser?>(new COMException("simulated"));
        public Task UpdateAsync(ApplicationUser entity) => Task.FromException(new COMException("simulated"));
    }

    public TestContext TestContext { get; set; }
}
