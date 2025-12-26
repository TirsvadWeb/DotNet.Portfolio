using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Portfolio.Infrastructure.Persistents;

using System.Data.Common;
using System.Diagnostics;

namespace Portfolio.Infrastructure.Tests.Persistents;

[TestClass]
public class ApplicationDbContextTests
{
    private const string dbPrefixName = "appdb_test_";
    private const string portfolioUserSecretsId = "0cf4f171-3a18-4cbc-a691-09a51dbb2c5e";

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string environment, string? name = null)
    {
        IConfiguration config = BuildConfiguration(environment);

        string? hostOverride = Environment.GetEnvironmentVariable("DB_HOST");
        string? host = string.IsNullOrWhiteSpace(hostOverride) ? config["Database:Host"] : hostOverride;
        string? port = config["Database:Port"];
        string? username = config["Database:Username"];
        string? password = config["Database:Password"];
        string? dbName = name
            ?? config[$"Database:Name:{environment}"]
            ?? config["Database:Name"];

        string connectionString = ResolveConnectionString(config, environment, host, port, username, password, dbName);

        SqlConnectionStringBuilder builder = new(connectionString);
        if (!string.IsNullOrWhiteSpace(host))
        {
            builder.DataSource = string.IsNullOrWhiteSpace(port) ? host : $"{host},{port}";
        }
        if (!string.IsNullOrWhiteSpace(dbName))
        {
            builder.InitialCatalog = dbName;
        }
        if (!string.IsNullOrWhiteSpace(username))
        {
            builder.UserID = username;
            Debug.WriteLine($"Using DB Username: {username}");
        }
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
            Debug.WriteLine($"Using DB Password: {"".PadLeft(password.Length, '*')}");
        }
        builder.TrustServerCertificate = true;

        Debug.WriteLine($"Using DB Connection String: {builder.ConnectionString}\n\n\n");

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(builder.ConnectionString + ';')
            .Options;
    }

    private static IConfiguration BuildConfiguration(string environment)
    {
        ConfigurationBuilder builder = new();

        string? portfolioDir = FindPortfolioProjectDirectory();
        if (!string.IsNullOrWhiteSpace(portfolioDir))
        {
            builder.SetBasePath(portfolioDir);
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            if (!string.IsNullOrWhiteSpace(environment))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

                if (string.Equals(environment, "Release", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: false);
                }
            }
        }

        builder.AddUserSecrets(userSecretsId: portfolioUserSecretsId, reloadOnChange: false);
        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    private static string? FindPortfolioProjectDirectory()
    {
        string? dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrWhiteSpace(dir))
        {
            string candidate = Path.Combine(dir, "src", "Portfolio", "Portfolio", "appsettings.json");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate);
            }

            DirectoryInfo? parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }

        return null;
    }

    private static string ResolveConnectionString(IConfiguration config, string environment, string? host, string? port, string? username, string? password, string? database)
    {
        List<string> envCandidates = [];
        if (!string.IsNullOrWhiteSpace(environment))
        {
            envCandidates.Add(environment);

            if (string.Equals(environment, "Release", StringComparison.OrdinalIgnoreCase))
            {
                envCandidates.Add("Production");
            }
        }

        envCandidates.Add("Default");

        foreach (string envCandidate in envCandidates)
        {
            string candidateName = $"{envCandidate}Connection";
            string? envDatabase = config[$"Database:Name:{envCandidate}"] ?? database;

            string? fromConnSection = config.GetConnectionString(candidateName);
            if (!string.IsNullOrWhiteSpace(fromConnSection))
            {
                return ReplaceTokens(fromConnSection, host, port, username, password, envDatabase, envCandidate);
            }

            string? fromDatabaseSection = config[$"Database:ConnectionString:{envCandidate}"];
            if (!string.IsNullOrWhiteSpace(fromDatabaseSection))
            {
                return ReplaceTokens(fromDatabaseSection, host, port, username, password, envDatabase, envCandidate);
            }
        }

        string? defaultConn = config["Database:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(defaultConn))
        {
            return ReplaceTokens(defaultConn, host, port, username, password, database, environment);
        }

        string safeHost = host ?? "localhost";
        string safePort = string.IsNullOrWhiteSpace(port) ? "1433" : port;
        string safeDatabase = database ?? "Portfolio";

        return $"Server={safeHost},{safePort};Database={safeDatabase};User Id={username};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }

    private static string ReplaceTokens(string template, string? host, string? port, string? username, string? password, string? database, string? environment = null)
    {
        Dictionary<string, string?> tokens = new(StringComparer.OrdinalIgnoreCase)
        {
            ["{Database:Host}"] = host,
            ["{Database:Port}"] = port,
            ["{Database:Username}"] = username,
            ["{Database:Password}"] = password,
            ["{Database:Name}"] = database,
        };

        if (!string.IsNullOrWhiteSpace(environment))
        {
            tokens[$"{{Database:Name:{environment}}}"] = database;
        }

        string result = template;
        foreach ((string token, string? value) in tokens)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                result = result.Replace(token, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    [TestMethod]
    [DataRow("Development")]
    [DataRow("Release")]
    [DataRow("Test")]
    [TestCategory("Functional")]
    public async Task ConnectionString_IsSet_For_Environment(string env)
    {
        // Ensure CreateOptions reads the intended environment value used by the test
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);

        DbContextOptions<ApplicationDbContext> options = CreateOptions(env);

        using ApplicationDbContext ctx = new(options);
        // Avoid performing an actual database migration during a connection-string-only unit test.
        // Migration requires a reachable SQL Server with matching credentials and causes brittle failures
        // when running in environments without the test database. The intent of this test is to
        // validate the resolved connection string, not to modify the database schema.
        //await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        //ctx.Database.MigrateAsync(TestContext.CancellationToken).GetAwaiter().GetResult();

        string conn = ctx.Database.GetDbConnection().ConnectionString ?? string.Empty;

        // Validate that the connection string is not empty and is a valid SQL Server connection string
        Assert.IsFalse(string.IsNullOrWhiteSpace(conn), $"Expected non-empty connection string for env '{env}'");
        try
        {
            SqlConnectionStringBuilder sb = new(conn);
            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.DataSource), $"Connection string missing Data Source for env '{env}'");
            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.InitialCatalog), $"Connection string missing Initial Catalog (database) for env '{env}'");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Connection string for env '{env}' is not a valid SQL Server connection string: {ex.Message}");
        }
    }

    [TestMethod]
    [DataRow("Development")]
    [DataRow("Release")]
    [DataRow("Test")]
    [TestCategory("Integration")]
    public async Task ConnectionString_CanOpenAndQueryDatabase_For_Environment(string env)
    {
        // Ensure CreateOptions reads the intended environment value used by the test
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);

        // Skip integration tests unless a test database is explicitly available/configured.
        // Use either the TEST_DATABASE_AVAILABLE env var set to "true", or ensure DB_HOST, DB_USERNAME and DB_PASSWORD
        // are provided via environment variables or configuration. This avoids running migrations in CI agents
        // that do not have a test database provisioned.
        //SkipIfNoIntegrationDatabase(env);

        //string dbName = $"appdb_access_{env.ToLowerInvariant()}_{Guid.NewGuid():N}";
        DbContextOptions<ApplicationDbContext> options = CreateOptions(env);

        using ApplicationDbContext ctx = new(options);
        //await ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken);
        //ctx.Database.MigrateAsync(TestContext.CancellationToken).GetAwaiter().GetResult();

        DbConnection conn = ctx.Database.GetDbConnection();
        Debug.WriteLine($"ConnectionString = {conn.ConnectionString}\n\n\n");
        try
        {
            await conn.OpenAsync(TestContext.CancellationToken);

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            object? result = await cmd.ExecuteScalarAsync(TestContext.CancellationToken);

            Assert.IsNotNull(result, $"Expected a result when querying DB for env '{env}'");

            int intResult = Convert.ToInt32(result);
            Assert.AreEqual(1, intResult, $"Expected query to return 1 for env '{env}'");
        }
        finally
        {
            try { conn.Close(); } catch { }
        }
    }

    private static void SkipIfNoIntegrationDatabase(string environment)
    {
        //string? flag = Environment.GetEnvironmentVariable("TEST_DATABASE_AVAILABLE");
        //if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase))
        //{
        //    return; // explicit opt-in
        //}

        // Require explicit opt-in via TEST_DATABASE_AVAILABLE or explicit environment variables only.
        // Do not treat user-secrets or config files as sufficient to enable integration tests.
        // Only consider environment variables here to decide whether to run integration tests.
        string? hostEnv = Environment.GetEnvironmentVariable("DB_HOST");
        string? userEnv = Environment.GetEnvironmentVariable("DB_USERNAME") ?? Environment.GetEnvironmentVariable("DB_USER");
        string? passEnv = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("DB_PASS");

        IConfiguration config = BuildConfiguration(environment);
        string? hostCfg = config["Database:Host"];
        string? userCfg = config["Database:Username"];
        string? passCfg = config["Database:Password"];

        string? host = string.IsNullOrWhiteSpace(hostEnv) ? hostCfg : hostEnv;
        string? user = string.IsNullOrWhiteSpace(userEnv) ? userCfg : userEnv;
        string? pass = string.IsNullOrWhiteSpace(passEnv) ? passCfg : passEnv;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            if (string.IsNullOrWhiteSpace(hostEnv) || string.IsNullOrWhiteSpace(userEnv) || string.IsNullOrWhiteSpace(passEnv))
            {
                // Use Assert.Skip to mark the test as skipped when a test DB is not configured.
                // Note: depending on MSTest version, `Assert.Skip` may not exist. If your test framework
                // does not provide `Assert.Skip`, revert to `Assert.Inconclusive` or use a custom skip handling.
                Assert.Inconclusive("Integration test skipped because test DB is not configured. Set TEST_DATABASE_AVAILABLE=true or provide DB_HOST, DB_USERNAME and DB_PASSWORD.");
                // Use Assert.Inconclusive to mark the test as skipped when a test DB is not configured.
                Assert.Inconclusive("Integration test skipped because test DB is not configured. Set TEST_DATABASE_AVAILABLE=true or provide DB_HOST, DB_USERNAME and DB_PASSWORD environment variables.");
            }
    }

    public TestContext TestContext { get; set; }
}
