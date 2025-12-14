using Portfolio.Core.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Tests;

[TestClass]
[DoNotParallelize]
public class X509CertificateServiceTests
{
    private const string PredefinedName = "TirsvadWebCert";
    private const string TestPrefix = "TST-";
    private readonly X509CertificateService _service = new();

    [TestInitialize]
    public void TestInitialize()
    {
        // Ensure a clean state before each test
        RemoveTestCertificates();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up any certificates we created during tests
        RemoveTestCertificates();
    }

    [TestMethod]
    public void GetCertificateByName_ReturnsCertificate_WhenFriendlyNameMatches()
    {
        string name = TestPrefix + "Friendly" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            X509Certificate2? found = _service.GetCertificateByName(name);

            Assert.IsNotNull(found, "Expected certificate to be found by friendly name.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, ignoreCase: true);
        }
        finally
        {
            cert.Dispose();
        }
    }

    [TestMethod]
    public void GetPreloadedCertificate_ReturnsCertificate_WhenPreloadedExists()
    {
        string name = PredefinedName;
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            X509Certificate2? found = _service.GetPreloadedCertificate();

            Assert.IsNotNull(found, "Expected preloaded certificate to be found when present.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, ignoreCase: true);
        }
        finally
        {
            cert.Dispose();
        }
    }

    [TestMethod]
    public void GetCertificateByName_ReturnsNull_ForInvalidInput()
    {
        Assert.IsNull(_service.GetCertificateByName(null!));
        Assert.IsNull(_service.GetCertificateByName(string.Empty));
        Assert.IsNull(_service.GetCertificateByName("   "));
    }

    [TestMethod]
    public void ThreadSafety_ConcurrentCalls_DoNotThrow_AndReturnConsistentResults()
    {
        string name = TestPrefix + "Concurrent" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            const int tasks = 50;
            X509Certificate2?[] results = new X509Certificate2?[tasks];
            List<Exception> exceptions = new();

            Parallel.For(0, tasks, i =>
            {
                try
                {
                    results[i] = _service.GetCertificateByName(name);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.IsEmpty(exceptions, "No exceptions should bubble up from concurrent calls.");
            Assert.IsTrue(results.All(r => r != null), "All concurrent calls should find the certificate.");
            var thumb = results.First(r => r != null)!.Thumbprint;
            Assert.IsTrue(results.All(r => r!.Thumbprint == thumb), "All results should report the same certificate thumbprint.");
        }
        finally
        {
            cert.Dispose();
        }
    }

    [TestMethod]
    public void Performance_MultipleCalls_CompleteWithinThreshold()
    {
        string name = TestPrefix + "Perf" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            const int calls = 100;
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < calls; i++)
            {
                _ = _service.GetCertificateByName(name);
            }

            sw.Stop();
            // Ensure 100 calls complete quickly; threshold set to 3s to be conservative on CI machines
            Assert.IsLessThan(3000, sw.ElapsedMilliseconds, $"Expected {calls} calls to complete under 3000ms but took {sw.ElapsedMilliseconds}ms.");
        }
        finally
        {
            cert.Dispose();
        }
    }

    [TestMethod]
    public void MultiSession_AddRemoveRepeat_ServiceBehavesCorrectly()
    {
        // Repeatedly add and remove certificate to ensure the service responds to new sessions
        string name = TestPrefix + "MultiSession" + Guid.NewGuid().ToString("N");

        for (int i = 0; i < 10; i++)
        {
            X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
            try
            {
                AddCertificateToStore(cert);
                X509Certificate2? found = _service.GetCertificateByName(name);
                Assert.IsNotNull(found, $"Iteration {i}: expected certificate to be found after adding.");
            }
            finally
            {
                cert.Dispose();
                RemoveCertificatesByFriendlyName(name);
            }

            X509Certificate2? missing = _service.GetCertificateByName(name);
            Assert.IsNull(missing, $"Iteration {i}: expected certificate to be not found after removal.");
        }
    }

    [TestMethod]
    public void Stress_DoesNotThrow_UnderConcurrentAddRemove()
    {
        // This test tries to stress the certificate store by concurrently adding/removing certs
        // while calling into the service to ensure exceptions are swallowed and no crash occurs.
        string nameBase = TestPrefix + "Stress" + Guid.NewGuid().ToString("N");

        const int workerCount = 8;
        const int iterations = 50;

        CancellationTokenSource tokenSource = new();
        List<Task> tasks = new();
        ConcurrentQueue<Exception> exceptions = new();

        // Start background workers that rapidly add and remove certificates
        for (int w = 0; w < workerCount; w++)
        {
            int workerId = w;
            tasks.Add(Task.Run(() =>
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
        tasks.Add(Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < workerCount * iterations; i++)
                {
                    // random existing or non-existing name
                    string name = (i % 2 == 0) ? (nameBase + "-0-" + (i % iterations)) : (nameBase + "-noexist-" + i);
                    try
                    {
                        _ = _service.GetCertificateByName(name);
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

        Task.WaitAll(tasks.ToArray());

        Assert.IsTrue(exceptions.IsEmpty, "No exceptions should escape during concurrent add/remove and service calls.");
    }

    [TestMethod]
    public void CacheInvalidation_PositiveEntry_RemovedFromStore_ServiceReturnsNullAfterRemoval()
    {
        string name = TestPrefix + "CachePos" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // First call should find and cache the certificate
            X509Certificate2? found = _service.GetCertificateByName(name);
            Assert.IsNotNull(found, "Expected certificate to be found and cached after adding.");

            // Remove from store and ensure subsequent lookup does not return a stale cached copy
            RemoveCertificatesByFriendlyName(name);
            Thread.Sleep(50); // small delay to allow store changes to be observed

            X509Certificate2? missing = _service.GetCertificateByName(name);
            Assert.IsNull(missing, "Expected certificate to be not found after removal (cache invalidated).");
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public void CacheInvalidation_NegativeEntry_NewlyAdded_Discovered()
    {
        string name = TestPrefix + "CacheNeg" + Guid.NewGuid().ToString("N");
        // Ensure negative result is cached
        X509Certificate2? notFound = _service.GetCertificateByName(name);
        Assert.IsNull(notFound, "Expected initial lookup to be null for a non-existent certificate.");

        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);
            Thread.Sleep(50); // allow store to update

            // Service should discover the newly added cert even if a null was cached previously
            X509Certificate2? found = _service.GetCertificateByName(name);
            Assert.IsNotNull(found, "Expected service to discover newly added certificate despite prior negative cache.");
            Assert.AreEqual(cert.Thumbprint, found!.Thumbprint, ignoreCase: true);
        }
        finally
        {
            cert.Dispose();
            RemoveCertificatesByFriendlyName(name);
        }
    }

    [TestMethod]
    public void ConcurrentCache_WarmCache_ManyReadersReturnSameThumbprint()
    {
        string name = TestPrefix + "CacheConcurrent" + Guid.NewGuid().ToString("N");
        X509Certificate2 cert = CreateSelfSignedCertificate(name, friendlyName: name);
        try
        {
            AddCertificateToStore(cert);

            // Warm the cache
            X509Certificate2? warm = _service.GetCertificateByName(name);
            Assert.IsNotNull(warm, "Expected warmup lookup to find the certificate.");
            var expectedThumb = warm!.Thumbprint;

            const int readers = 100;
            X509Certificate2?[] results = new X509Certificate2?[readers];
            List<Exception> exceptions = new();

            Parallel.For(0, readers, i =>
            {
                try
                {
                    results[i] = _service.GetCertificateByName(name);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

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
    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName, string? friendlyName = null)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest req = new($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 cert = req.CreateSelfSigned(now.AddDays(-1), now.AddYears(1));

        // Export and re-import to ensure private key is persisted and usable when added to store
        var pfx = cert.Export(X509ContentType.Pfx);

        // Use X509CertificateLoader.Load (note: the API is 'Load', not 'LoadFrom')
        X509Certificate2 cert2 = X509CertificateLoader.LoadPkcs12(
            pfx,
            string.Empty,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

        if (!string.IsNullOrEmpty(friendlyName))
        {
            try
            {
                // Only attempt to set FriendlyName when running on Windows where it's supported
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    cert2.FriendlyName = friendlyName;
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

    private static void RemoveCertificatesByFriendlyName(string friendlyName)
    {
        try
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            List<X509Certificate2> matches = store.Certificates.Cast<X509Certificate2>().Where(c => string.Equals(c.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (X509Certificate2? c in matches)
            {
                try { store.Remove(c); } catch { }
                try { c.Dispose(); } catch { }
            }
            store.Close();
        }
        catch { }
    }

    private static void RemoveTestCertificates()
    {
        try
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            List<X509Certificate2> matches = store.Certificates.Cast<X509Certificate2>().Where(c =>
                !string.IsNullOrEmpty(c.FriendlyName) && (c.FriendlyName.StartsWith(TestPrefix, StringComparison.OrdinalIgnoreCase) || c.FriendlyName.Equals(PredefinedName, StringComparison.OrdinalIgnoreCase))
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
}
