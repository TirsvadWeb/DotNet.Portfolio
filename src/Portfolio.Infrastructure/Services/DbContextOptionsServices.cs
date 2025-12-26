using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Portfolio.Infrastructure.Persistents;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Portfolio.Infrastructure.Services;

public class DbContextOptionsServices : IDbContextOptionsServices
{
    private readonly string? _portfolioUserSecretsId;

    public DbContextOptionsServices(string? userSecretsId = null)
    {
        // If caller didn't provide a user secrets id, try to read it from appsettings files
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("ASPNETCORE_ENVIRONMENT not set");
        _portfolioUserSecretsId = userSecretsId ?? GetUserSecretsIdFromAppSettings(env);
    }

    public DbContextOptions<ApplicationDbContext> CreateOptions(string environment, string? name = null)
    {
        string conn = CreateConnectionString(environment, name);

        SqlConnectionStringBuilder builder = new(conn)
        {
            // connection string resolver already sets data source and initial catalog if provided
            TrustServerCertificate = true
        };

        Debug.WriteLine($"Using DB Connection String: {builder.ConnectionString}");

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;
    }

    public string CreateConnectionString(string environment, string? name = null)
    {
        IConfiguration config = BuildConfiguration(environment);
        string? hostOverride = Environment.GetEnvironmentVariable("DB_HOST");
        string? portOverride = Environment.GetEnvironmentVariable("DB_PORT");
        string? usernameOverride = Environment.GetEnvironmentVariable("DB_USERNAME");
        string? passwordOverride = Environment.GetEnvironmentVariable("DB_PASSWORD");
        string? dbNameOverride = Environment.GetEnvironmentVariable("DB_NAME");
        string? host = string.IsNullOrWhiteSpace(hostOverride) ? config["Database:Host"] : hostOverride;
        string? port = string.IsNullOrWhiteSpace(portOverride) ? config["Database:Port"] : portOverride;
        string? username = string.IsNullOrWhiteSpace(usernameOverride) ? config["Database:Username"] : usernameOverride;
        string? password = string.IsNullOrWhiteSpace(passwordOverride) ? config["Database:Password"] : passwordOverride;
        string? dbName = name
            ?? dbNameOverride
            ?? config[$"Database:Default:Name:{environment}"]
            ?? config["Database:Default:Name"];

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
            DebugInfo($"Using DB Username: {username}");
        }
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
            DebugInfo($"Using DB Password: {"".PadLeft(password.Length, '*')}");
        }

        builder.TrustServerCertificate = true;

        return builder.ConnectionString;
    }

    private IConfiguration BuildConfiguration(string environment)
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

        if (!string.IsNullOrWhiteSpace(_portfolioUserSecretsId))
        {
            try
            {
                builder.AddUserSecrets(userSecretsId: _portfolioUserSecretsId, reloadOnChange: false);
            }
            catch (ArgumentNullException ane)
            {
                // Defensive: ignore and continue building configuration if the provider throws on null/invalid id
                DebugInfo($"Warning: AddUserSecrets threw ArgumentNullException: {ane.Message}");
            }
            catch (Exception ex)
            {
                DebugInfo($"Warning: failed to add user secrets: {ex.Message}");
            }
        }

        builder.AddEnvironmentVariables();

        return builder.Build();

    }

    private static string? FindPortfolioProjectDirectory()
    {
        // Use AppContext.BaseDirectory to get a list of subdirectories if available, but guard against exceptions
        string baseDir = AppContext.BaseDirectory ?? string.Empty;
        string[] baseLocation = Array.Empty<string>();
        string[] assambplePath = Array.Empty<string>();
        try
        {
            if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
            {
                string rs = Path.GetDirectoryName(typeof(DbContextOptionsServices).Assembly.Location) ?? AppContext.BaseDirectory ?? string.Empty;
                // Get the directory containing the current assembly as a reliable starting point
                string assemblyDirectory = Path.GetDirectoryName(typeof(DbContextOptionsServices).Assembly.Location) ?? AppContext.BaseDirectory ?? string.Empty;

                AssemblyName assemblyName = typeof(DbContextOptionsServices).Assembly.GetName();
                DebugInfo($"Assembly Name: {assemblyName.Name}");

                // Normalize and split the assembly directory into path segments
                string[] parts = assemblyDirectory
                    .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                // Keep the last few segments to help with diagnosis/fallback
                string[] lastParts = parts.Skip(Math.Max(0, parts.Length - 3)).ToArray();

                if (assemblyName.Name is null)
                {
                    return string.Empty;
                }
                assambplePath = parts.RemoveBefore(p => !string.IsNullOrEmpty(p) && p.StartsWith(assemblyName.Name, StringComparison.OrdinalIgnoreCase), includeMatch: false);
                baseLocation = parts.RemoveAfter(p => !string.IsNullOrEmpty(p) && p.StartsWith(assemblyName.Name, StringComparison.OrdinalIgnoreCase), includeMatch: false);
                DebugInfo($"Base location: {string.Join(Path.DirectorySeparatorChar.ToString(), baseLocation)}", typeof(DbContextOptionsServices).Namespace);
                DebugInfo($"Assembly path parts: {string.Join(", ", lastParts)}", typeof(DbContextOptionsServices).Namespace);

                string newPath = Path.Combine(baseLocation)
                    + Path.DirectorySeparatorChar
                    + string.Join(Path.DirectorySeparatorChar.ToString(), "Portfolio")
                    + Path.DirectorySeparatorChar + Path.Combine(assambplePath);

                DebugInfo($" - Assembly directory: {assemblyDirectory}", typeof(DbContextOptionsServices).Namespace);

                return newPath;
            }
        }
        catch
        {
            // ignore any access issues
        }

        return string.Empty;
    }

    private static string ResolveConnectionString(IConfiguration config, string environment, string? host, string? port, string? username, string? password, string? database)
    {
        List<string> envCandidates = new();
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

    public static string? GetUserSecretsIdFromAppSettings(string? environment = null)
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

        IConfiguration config = builder.Build();

        // Common possible locations for a user secrets id in appsettings
        string? value = config["UserSecretsId"]
            ?? config["UserSecrets:Id"]
            ?? config["UserSecrets:UserSecretsId"]
            ?? config["Portfolio:UserSecretsId"];

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static string[] DiscardValueFromArray(string[] originalArray, string? discardFrom = null, string? discardUntil = null, bool includeDiscardValue = false)
    {
        List<string> list = new(originalArray);

        list = DiscardValueFromList(list, discardFrom!, discardAfter: true, includeDiscardValue);
        list = DiscardValueFromList(list, discardUntil!, discardAfter: false, includeDiscardValue);

        DebugInfo($"Original Array: {string.Join(", ", originalArray)}", nameof(DbContextOptionsServices));
        DebugInfo($"Value to discard from: {discardFrom}", nameof(DbContextOptionsServices));
        DebugInfo($"New Array: {string.Join(", ", list)}", nameof(DbContextOptionsServices));
        return list.ToArray();
    }

    public static List<string> DiscardValueFromList(List<string> originalList, string discardValue, bool discardAfter = true, bool includeDiscardValue = false)
    {
        List<string> list = new(originalList);
        if (string.IsNullOrEmpty(discardValue))
        {
            return list;
        }

        int index = list.FindIndex(s => !string.IsNullOrEmpty(s) && s.StartsWith(discardValue!, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return list;
        }

        if (discardAfter)
        {
            int start = includeDiscardValue ? index : index + 1;
            if (start < list.Count)
            {
                list.RemoveRange(start, list.Count - start);
            }
        }
        else
        {
            int count = includeDiscardValue ? index + 1 : index;
            if (count > 0)
            {
                list.RemoveRange(0, Math.Min(count, list.Count));
            }
        }

        return list;
    }


    private static void DebugInfo(string message, string? className = null, [CallerMemberName] string memberName = "")
    {
        if (className is not null)
        {
            Debug.WriteLine($"{className}.{memberName} - {message}");
            return;
        }
        Debug.WriteLine($"{memberName} - {message}");
    }

}

public static class DbContextOptionsServicesExtensions
{
    public static T[] RemoveAfter<T>(this T[] source, Func<T, bool> predicate, bool includeMatch = false)
    {
        if (source is null || predicate is null)
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(predicate));

        List<T> list = new(source);
        int index = list.FindIndex(item => predicate(item));
        if (index < 0)
        {
            return source;
        }

        if (includeMatch)
        {
            list.RemoveRange(index + 1, list.Count - index - 1);
        }
        else
        {
            list.RemoveRange(index, list.Count - index);
        }

        return list.ToArray();
    }

    public static T[] RemoveBefore<T>(this T[] source, Func<T, bool> predicate, bool includeMatch = false)
    {
        if (source is null || predicate is null)
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(predicate));

        List<T> list = new(source);
        int index = list.FindLastIndex(item => predicate(item));
        if (index < 0)
        {
            return source;
        }

        if (includeMatch)
        {
            list.RemoveRange(0, index);
        }
        else
        {
            list.RemoveRange(0, index + 1);
        }

        return list.ToArray();
    }
}
