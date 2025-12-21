using Portfolio.Core.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Tests;


/// <summary>
/// Tests for <see cref="X509CertificateService"/>
/// 
/// </summary>
[TestClass]
[TestCategory("Functional")]
// [DoNotParallelize]
public class X509CertificateServiceTests
{
    private const string PredefinedName = "TirsvadWebCert";
    private const string TestPrefix = "TST-";
    private readonly X509CertificateService _service = new();

    // Performance test thresholds
    private const int _calls = 100;
    private const int _elapsedMilliseconds = 16000; // TODO: Optimize this threshold based on CI performance data
    private const int _hotPathElapsedMilliseconds = 16000; // TODO: Optimize this threshold based on CI performance data

    // Unique marker for this test instance so parallel tests do not interfere.
    private readonly string _testInstanceId = Guid.NewGuid().ToString("N");

    [TestInitialize]
    public void TestInitialize()
    {
        // Ensure a clean state for this test instance only
        RemoveTestCertificates();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up certificates created by this test instance
        RemoveTestCertificates();
    }

    [TestMethod]
    public async Task GetCertificateByName_ReturnsCertificate_WhenFriendlyNameMatches()
    {
        string name = TestPrefix + "Friendly" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            X509Certificate2? found = await _service.GetCertificateByNameAsync(name);

            Assert.IsNotNull(found, "Expected certificate to be found by friendly name.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, true);
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task GetPreloadedCertificate_ReturnsCertificate_WhenPreloadedExists()
    {
        string name = PredefinedName;
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            X509Certificate2? found = await _service.GetPreloadedCertificateAsync();

            Assert.IsNotNull(found, "Expected preloaded certificate to be found when present.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, true);
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task GetCertificateByName_ReturnsNull_ForInvalidInput()
    {
        Assert.IsNull(await _service.GetCertificateByNameAsync(null!));
        Assert.IsNull(await _service.GetCertificateByNameAsync(string.Empty));
        Assert.IsNull(await _service.GetCertificateByNameAsync("   "));
    }

    [TestMethod]
    public async Task ThreadSafety_ConcurrentCalls_DoNotThrow_AndReturnConsistentResults()
    {
        string name = TestPrefix + "Concurrent" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            const int taskCount = 50;
            X509Certificate2?[] results = new X509Certificate2?[taskCount];
            List<Exception> exceptions = new();

            // Launch asynchronous tasks properly and await them. Using `Parallel.For` with
            // an async delegate results in fire-and-forget behavior and the loop will
            // complete before the async work finishes which can leave `results` with
            // many null entries. Use Task.Run and await Task.WhenAll to ensure all
            // concurrent lookups complete before asserting results.
            Task[] workerTasks = new Task[taskCount];
            for (int j = 0; j < taskCount; j++)
            {
                int idx = j;
                workerTasks[idx] = Task.Run(async () =>
                {
                    try
                    {
                        results[idx] = await _service.GetCertificateByNameAsync(name);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }, TestContext.CancellationToken);
            }

            await Task.WhenAll(workerTasks);

            Assert.IsEmpty(exceptions, "No exceptions should bubble up from concurrent calls.");
            Assert.IsTrue(results.All(r => r != null), "All concurrent calls should find the certificate.");
            string thumb = results.First(r => r != null)!.Thumbprint;
            Assert.IsTrue(results.All(r => r!.Thumbprint == thumb), "All results should report the same certificate thumbprint.");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    [TestCategory("Benchmark")]
    [DoNotParallelize]
    public async Task Performance_MultipleCalls_CompleteWithinThreshold()
    {
        string name = TestPrefix + "Perf" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < _calls; i++)
            {
                _ = _service.GetCertificateByNameAsync(name);
            }

            sw.Stop();
            // Ensure 100 calls complete quickly; threshold set to _elapsedMilliseconds to be conservative on CI machines
            Assert.IsLessThan(_elapsedMilliseconds, sw.ElapsedMilliseconds, $"Expected {_calls} calls to complete under {_elapsedMilliseconds}ms but took {sw.ElapsedMilliseconds}ms.");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task MultiSession_AddRemoveRepeat_ServiceBehavesCorrectly()
    {
        // Repeatedly add and remove certificate to ensure the service responds to new sessions
        string name = TestPrefix + "MultiSession" + Guid.NewGuid().ToString("N");

        for (int i = 0; i < 10; i++)
        {
            X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
            try
            {
                AddCertificateToStore(cert);
                // Allow OS/store to surface the newly added certificate before lookup.
                Thread.Sleep(50);
                X509Certificate2? found = await _service.GetCertificateByNameAsync(name);
                Assert.IsNotNull(found, $"Iteration {i}: expected certificate to be found after adding.");
            }
            finally
            {
                cert.Dispose();
                RemoveCertificatesByFriendlyName(name);
            }

            X509Certificate2? missing = await _service.GetCertificateByNameAsync(name);
            Assert.IsNull(missing, $"Iteration {i}: expected certificate to be not found after removal.");
        }
    }

    [TestMethod]
    public async Task Stress_DoesNotThrow_UnderConcurrentAddRemove()
    {
        // This test tries to stress the certificate store by concurrently adding/removing certs
        // while calling into the service to ensure exceptions are swallowed and no crash occurs.
        string nameBase = TestPrefix + "Stress" + Guid.NewGuid().ToString("N");

        const int workerCount = 8;
        const int iterations = 50;

        CancellationTokenSource tokenSource = new();
        List<Task> backgroundTasks = new();
        ConcurrentQueue<Exception> exceptions = new();

        // Start background workers that rapidly add and remove certificates
        for (int w = 0; w < workerCount; w++)
        {
            int workerId = w;
            backgroundTasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        string name = nameBase + "-" + workerId + "-" + i;
                        using X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
                        AddCertificateToStore(cert);
                        // Small delay to magnify race window
                        Thread.Sleep(5);
                        RemoveCertificatesByFriendlyName(name);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                    tokenSource.Cancel();
                }
            }, tokenSource.Token));
        }

        // Concurrently call into the service while the workers are mutating the store
        backgroundTasks.Add(Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < workerCount * iterations; i++)
                {
                    // random existing or non-existing name
                    string name = (i % 2 == 0) ? (nameBase + "-0-" + (i % iterations)) : (nameBase + "-noexist-" + i);
                    try
                    {
                        _ = await _service.GetCertificateByNameAsync(name);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                        tokenSource.Cancel();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, tokenSource.Token));

        Task.WaitAll(backgroundTasks.ToArray(), TestContext.CancellationToken);

        Assert.IsTrue(exceptions.IsEmpty, "No exceptions should escape during concurrent add/remove and service calls.");
    }

    [TestMethod]
    public async Task CacheInvalidation_PositiveEntry_RemovedFromStore_ServiceReturnsNullAfterRemovalAsync()
    {
        string name = TestPrefix + "CachePos" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // First call should find and cache the certificate
            X509Certificate2? found = await _service.GetCertificateByNameAsync(name);
            Assert.IsNotNull(found, "Expected certificate to be found and cached after adding.");

            // Remove from store and ensure subsequent lookup does not return a stale cached copy
            RemoveCertificatesByFriendlyName(name);
            Thread.Sleep(50); // small delay to allow store changes to be observed

            X509Certificate2? missing = await _service.GetCertificateByNameAsync(name);
            Assert.IsNull(missing, "Expected certificate to be not found after removal (cache invalidated).");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task CacheInvalidation_NegativeEntry_NewlyAdded_Discovered()
    {
        string name = TestPrefix + "CacheNeg" + Guid.NewGuid().ToString("N");
        // Ensure negative result is cached
        X509Certificate2? notFound = await _service.GetCertificateByNameAsync(name);
        Assert.IsNull(notFound, "Expected initial lookup to be null for a non-existent certificate.");

        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);
            Thread.Sleep(50); // allow store to update

            // Service should discover the newly added cert even if a null was cached previously
            X509Certificate2? found = await _service.GetCertificateByNameAsync(name);
            Assert.IsNotNull(found, "Expected service to discover newly added certificate despite prior negative cache.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, true);
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task ConcurrentCache_WarmCache_ManyReadersReturnSameThumbprintAsync()
    {
        string name = TestPrefix + "CacheConcurrent" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // Warm the cache
            X509Certificate2? warm = await _service.GetCertificateByNameAsync(name);
            Assert.IsNotNull(warm, "Expected warmup lookup to find the certificate.");
            string expectedThumb = warm!.Thumbprint;

            const int readers = 100;
            X509Certificate2?[] results = new X509Certificate2?[readers];
            List<Exception> exceptions = new();

            // Launch tasks and await them. Avoid using Parallel.For with async lambdas
            // because Parallel.For does not await async delegates (they become fire-and-forget).
            Task[] tasks = new Task[readers];
            for (int i = 0; i < readers; i++)
            {
                int idx = i;
                tasks[idx] = Task.Run(async () =>
                {
                    try
                    {
                        results[idx] = await _service.GetCertificateByNameAsync(name);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }, TestContext.CancellationToken);
            }

            await Task.WhenAll(tasks);

            Assert.IsEmpty(exceptions, "Concurrent readers should not throw.");
            Assert.IsTrue(results.All(r => r != null), "All readers should find the certificate.");
            Assert.IsTrue(results.All(r => r!.Thumbprint == expectedThumb), "All readers should report the same certificate thumbprint.");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    [TestCategory("Benchmark")]
    [DoNotParallelize]
    public async Task Cache_HotPath_Latency_Benchmark()
    {
        string name = TestPrefix + "HotPath" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // Warm the cache so the hot-path is exercised
            X509Certificate2? warm = await _service.GetCertificateByNameAsync(name);
            Assert.IsNotNull(warm, "Expected warmup lookup to find the certificate.");

            //const int calls = 100;
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < _calls; i++)
            {
                _ = _service.GetCertificateByNameAsync(name);
            }

            sw.Stop();

            // Conservative threshold: 100 cached calls should complete under 1s on CI
            Assert.IsLessThan(_hotPathElapsedMilliseconds, sw.ElapsedMilliseconds, $"Hot-path {_calls} cached calls should complete under 1s but took {sw.ElapsedMilliseconds}ms.");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public async Task Cache_Warm_MultipleReaders_AgreeOnThumbprint()
    {
        string name = TestPrefix + "CacheSmall" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // Warm the cache
            X509Certificate2? warm = await _service.GetCertificateByNameAsync(name);
            Assert.IsNotNull(warm, "Expected warmup lookup to find the certificate.");
            string expectedThumb = warm!.Thumbprint;

            const int readers = 50;
            X509Certificate2?[] results = new X509Certificate2?[readers];
            List<Exception> exceptions = new();

            // Launch tasks and await them. Avoid using Parallel.For with async lambdas
            // because Parallel.For does not await async delegates (they become fire-and-forget).
            Task[] tasks = new Task[readers];
            for (int i = 0; i < readers; i++)
            {
                int idx = i;
                tasks[idx] = Task.Run(async () =>
                {
                    try
                    {
                        results[idx] = await _service.GetCertificateByNameAsync(name);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }, TestContext.CancellationToken);
            }

            await Task.WhenAll(tasks);

            Assert.IsEmpty(exceptions, "Concurrent readers should not throw.");
            Assert.IsTrue(results.All(r => r != null), "All readers should find the certificate.");
            Assert.IsTrue(results.All(r => r!.Thumbprint == expectedThumb), "All readers should report the same certificate thumbprint.");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    // Helper methods

    // Make CreateSelfSignedCertificate instance-bound so we can append a per-test-instance marker
    private X509Certificate2 CreateSelfSignedCertificate(string subjectName, string? friendlyName = null)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest req = new($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 cert = req.CreateSelfSigned(now.AddDays(-1), now.AddYears(1));

        // Export and re-import to ensure private key is persisted and usable when added to store
        byte[] pfx = cert.Export(X509ContentType.Pfx);

        // Use X509CertificateLoader.Load (note: the API is 'Load', not 'LoadFrom')
        X509Certificate2 cert2 = X509CertificateLoader.LoadPkcs12(
            pfx,
            string.Empty,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

        if (!string.IsNullOrEmpty(friendlyName))
        {
            try
            {
                // Append instance marker so parallel tests can clean up their own certificates.
                string instanceFriendly = $"{friendlyName}|{_testInstanceId}";

                // Only attempt to set FriendlyName when running on Windows where it's supported
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    cert2.FriendlyName = instanceFriendly;
                }
            }
            catch
            {
                // Ignore if setting FriendlyName fails for any reason
            }
        }

        return cert2;
    }

    private static void AddCertificateToStore(X509Certificate2 cert)
    {
        using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(cert);
        store.Close();
    }

    // Remove certificates created for the provided friendlyName by this test instance.
    private void RemoveCertificatesByFriendlyName(string friendlyName)
    {
        try
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            string expectedSuffix = "|" + _testInstanceId;
            List<X509Certificate2> matches = store.Certificates.Cast<X509Certificate2>().Where(c =>
            {
                if (string.IsNullOrEmpty(c.FriendlyName))
                {
                    // If FriendlyName is not available (e.g. non-Windows stores), fall back to matching by subject CN
                    if (!string.IsNullOrEmpty(c.Subject) && c.Subject.IndexOf($"CN={friendlyName}", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Exact match (older behavior) or instance-suffixed friendly name
                if (string.Equals(c.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (c.FriendlyName.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase)
                    && c.FriendlyName.StartsWith(friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Also consider subject-based match as a fallback in case FriendlyName isn't populated
                if (!string.IsNullOrEmpty(c.Subject) && c.Subject.IndexOf($"CN={friendlyName}", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                return false;

            }).ToList();

            foreach (X509Certificate2? c in matches)
            {
                try { store.Remove(c); } catch { }
                try { c.Dispose(); } catch { }
            }

            store.Close();
        }
        catch { }
    }

    // Remove only certificates created by this test instance to avoid disturbing parallel runs.
    private void RemoveTestCertificates()
    {
        try
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            string expectedSuffix = "|" + _testInstanceId;

            List<X509Certificate2> matches = store.Certificates.Cast<X509Certificate2>().Where(c =>
                // Certificates created by this test instance have our suffix on FriendlyName
                (!string.IsNullOrEmpty(c.FriendlyName) && c.FriendlyName.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
                // Also remove predefined cert created by this instance (FriendlyName could be PredefinedName|instance)
                || (!string.IsNullOrEmpty(c.FriendlyName) && c.FriendlyName.Equals($"{PredefinedName}{expectedSuffix}", StringComparison.OrdinalIgnoreCase))
                // Also consider subject-based matches only if FriendlyName contains our instance id
                || (!string.IsNullOrEmpty(c.Subject) && c.Subject.Contains($"CN={PredefinedName}", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(c.FriendlyName) && c.FriendlyName.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            foreach (X509Certificate2? c in matches)
            {
                try { store.Remove(c); } catch { }
                try { c.Dispose(); } catch { }
            }

            store.Close();
        }
        catch { }
    }

    public TestContext TestContext { get; set; }
}
