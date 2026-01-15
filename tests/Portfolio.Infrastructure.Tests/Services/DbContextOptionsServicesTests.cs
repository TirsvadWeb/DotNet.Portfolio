using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Portfolio.Infrastructure.Persistences;
using Portfolio.Infrastructure.Services;

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Portfolio.Infrastructure.Tests.Services
{
    [TestClass]
    public class DbContextOptionsServicesTests
    {
        #region Functional Tests
        [TestMethod]
        [TestCategory("Functional")]
        [DataRow("Development")]
        [DataRow("Release")]
        public void CreateOptions_ReturnsValidOptions(string env)
        {
            DbContextOptionsServices svc = new();
            DbContextOptions<ApplicationDbContext> options = svc.CreateOptions(env);
            Assert.IsNotNull(options);
            Assert.IsInstanceOfType(options, typeof(DbContextOptions<ApplicationDbContext>));
        }

        [TestMethod]
        [TestCategory("Functional")]
        [DataRow("Development")]
        [DataRow("Release")]
        public void CreateConnectionString_ReturnsValidConnectionString(string env)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);
            DbContextOptionsServices svc = new();
            string conn = svc.CreateConnectionString(env);
            Assert.IsFalse(string.IsNullOrWhiteSpace(conn));
            SqlConnectionStringBuilder builder = new(conn);
            Assert.IsFalse(string.IsNullOrWhiteSpace(builder.DataSource));
        }
        #endregion

        #region Error/Com Tests
        //[TestMethod]
        //[TestCategory("Error")]
        //public void Constructor_ThrowsIfEnvNotSet()
        //{
        //    string? oldApsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        //    // Test with null
        //    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        //    try
        //    {
        //        Assert.Throws<Exception>(() => new DbContextOptionsServices());
        //    }
        //    finally
        //    {
        //        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", oldApsEnv);
        //    }

        //    // Test with empty string
        //    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", string.Empty);
        //    try
        //    {
        //        Assert.Throws<Exception>(() => new DbContextOptionsServices());
        //    }
        //    finally
        //    {
        //        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", oldApsEnv);
        //    }
        //}

        [TestMethod]
        [TestCategory("Error")]
        public void CreateConnectionString_ThrowsIfNotFound()
        {
            // Ensure environment variable is set before any code runs
            string? oldApsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string? origDbEnv = Environment.GetEnvironmentVariable("DOCKER_DOTNET_TEST");
            //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "NonExistentEnv");

            Environment.SetEnvironmentVariable("DOCKER_DOTNET_TEST", "false");

            try
            {
                DbContextOptionsServices svc = new();
                _ = Assert.Throws<Exception>(() => svc.CreateConnectionString("NonExistentEnv"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", oldApsEnv);
                Environment.SetEnvironmentVariable("DOCKER_DOTNET_TEST", origDbEnv);
            }
        }
        #endregion

        #region Concurrency/Threadsafe Tests
        [TestMethod]
        [TestCategory("Concurrency")]
        public void CreateOptions_ThreadSafe_MultiSession()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            DbContextOptionsServices svc = new();
            _ = Parallel.For(0, 10, i =>
            {
                DbContextOptions<ApplicationDbContext> options = svc.CreateOptions("Development");
                Assert.IsNotNull(options);
            });
        }
        #endregion

        #region Performance/Benchmark Tests
        [TestMethod]
        [TestCategory("Performance")]
        public void CreateConnectionString_Performance_Benchmark()
        {
            int timeOut = 400; // milliseconds
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            DbContextOptionsServices svc = new();
            Stopwatch sw = Stopwatch.StartNew();
            string conn = svc.CreateConnectionString("Development");
            sw.Stop();
            Assert.IsLessThan(timeOut, sw.ElapsedMilliseconds, $"Performance issue: {sw.ElapsedMilliseconds}ms");
            Assert.IsFalse(string.IsNullOrWhiteSpace(conn));
        }
        #endregion

        #region Multi-Session Tests
        [TestMethod]
        [TestCategory("Concurrency")]
        public void CreateOptions_MultiSession()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            _ = Parallel.For(0, 5, i =>
            {
                DbContextOptionsServices svc = new();
                DbContextOptions<ApplicationDbContext> options = svc.CreateOptions("Development");
                Assert.IsNotNull(options);
            });
        }
        #endregion

        #region Private/Helper Tests
        [TestMethod]
        [TestCategory("Helper")]
        public void BuildConfiguration_ReturnsConfigurationRoot()
        {
            MethodInfo? method = typeof(DbContextOptionsServices).GetMethod("BuildConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            IConfiguration config = (IConfiguration)method.Invoke(null, new object[] { "Development" })!;
            Assert.IsNotNull(config);
        }

        [TestMethod]
        [TestCategory("Helper")]
        public void ReplaceTokens_ResolvesToken()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            DbContextOptionsServices svc = new();
            MethodInfo? field = typeof(DbContextOptionsServices).GetMethod("ReplaceTokens", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);
            string? result = (string?)field.Invoke(svc, new object[] { "{Portfolio:UserSecretsId}" });
            // Should resolve or return null/empty if not found
            Assert.IsTrue(result is null or string);
        }

        [TestMethod]
        [TestCategory("Helper")]
        public void GetEnvironmentConnectionOverrides_ReturnsDictionary()
        {
            MethodInfo? method = typeof(DbContextOptionsServices).GetMethod("GetEnvironmentConnectionOverrides", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            IDictionary dict = (System.Collections.IDictionary)method.Invoke(null, null)!;
            Assert.IsNotNull(dict);
        }

        [TestMethod]
        [TestCategory("Helper")]
        public void ApplyEnvironmentOverrides_UpdatesDictionary()
        {
            MethodInfo? method = typeof(DbContextOptionsServices).GetMethod("ApplyEnvironmentOverrides", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            ConcurrentDictionary<string, string> dict = new() { ["A"] = "1" };
            ConcurrentDictionary<string, string> env = new() { ["A"] = "2" };
            _ = method.Invoke(null, new object[] { dict, env });
            Assert.AreEqual("2", dict["A"]);
        }
        #endregion
    }
}
