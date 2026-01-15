using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Portfolio.Infrastructure.Abstacts;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Infrastructure.Persistences;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Portfolio.Infrastructure.Services;

/// <summary>
/// Provides services for building <see cref="DbContextOptions{TContext}"/> for <see cref="ApplicationDbContext"/>.
/// <para>
/// <b>Inheriting XML Documentation with &lt;inheritdoc/&gt;:</b><br/>
/// This class implements <see cref="IDbContextOptionsServices"/> and uses <c>&lt;inheritdoc/&gt;</c> to inherit documentation from the interface, ensuring consistency and reducing duplication.
/// </para>
/// <para>
/// <b>How to use:</b><br/>
/// <code language="csharp">
/// // Register in DI (Program.cs)
/// builder.Services.AddScoped&lt;IDbContextOptionsServices, DbContextOptionsServices&gt;();
///
/// // Resolve and use
/// var dbOptionsService = serviceProvider.GetRequiredService&lt;IDbContextOptionsServices&gt;();
/// var options = dbOptionsService.CreateOptions("Development");
/// using var db = new ApplicationDbContext(options);
/// </code>
/// </para>
/// <para>
/// <b>Why we have it:</b><br/>
/// Centralizes the logic for building DbContext options and connection strings, supporting multiple environments and secret management.
/// </para>
/// </summary>
public class DbContextOptionsServices : IDbContextOptionsServices
{
    #region fields
    // Holds the user secrets ID for portfolio, if any
    private readonly string? _portfolioUserSecretsId;
    // The current environment (e.g., Development, Production)
    private readonly string? _env;
    // The configuration root used for resolving settings
    private IConfiguration? _config;
    private static readonly Lock _optionsLock = new();
    #endregion

    #region constructors
    /// <inheritdoc/>
    public DbContextOptionsServices(string? userSecretsId = null)
    {
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        Debug.WriteLine($"[DEBUG] ASPNETCORE_ENVIRONMENT: {_env}");

        // Load appsettings configuration
        _config = BuildConfiguration(_env);

        // If caller didn't provide a user secrets id, try to read it from appsettings files
        _portfolioUserSecretsId = userSecretsId ?? _config!["Portfolio:UserSecretsId"];
    }
    #endregion

    #region public methods
    /// <inheritdoc/>
    public DbContextOptions<ApplicationDbContext> CreateOptions(string environment, string? name = null)
    {
        lock (_optionsLock)
        {
            environment ??= _env!;
            string conn = CreateConnectionString(environment, name);

            // Build the SQL connection string with trust server certificate
            SqlConnectionStringBuilder builder = new(conn)
            {
                TrustServerCertificate = true
            };

            Debug.WriteLine($"Using DB Connection String: {builder.ConnectionString}");

            // Return the configured DbContextOptions
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;
        }
    }

    /// <inheritdoc/>
    public string CreateConnectionString(string environment, string? name = null)
    {
        Debug.WriteLine($"Building connection string for environment: {environment}");
        string? envForDb = null;
        _config = BuildConfiguration(environment);

        string? envVar = Environment.GetEnvironmentVariable("DOCKER_DOTNET_TEST");
        Debug.WriteLine($"Current DOCKER_DOTNET_TEST: {envVar}");

        if (Environment.GetEnvironmentVariable("DOCKER_DOTNET_TEST") == "true")
        {
            envForDb = "Test";
        }


        string connectionsStringInit = $"ConnectionStrings:Database:Default:{envForDb ?? environment}";

        Debug.WriteLine($"Looking for Database conf in json path {connectionsStringInit}");

        // Try to resolve the connection string from several common locations.
        IConfigurationSection? connectionConfig = _config!.GetSection(connectionsStringInit);
        if (!connectionConfig.Exists())
            throw new Exception($"Connection string not found for environment: {environment}");

        Debug.WriteLine($"\tConnection Config Section: {connectionsStringInit}");

        ConcurrentDictionary<string, string> connectionDict = BuildConnectionDictionaryFromConfig(connectionConfig);
        List<string> connectionTags = [];

        ConcurrentDictionary<string, string> envOverrides = GetEnvironmentConnectionOverrides();
        bool envOverride = !envOverrides.IsEmpty;
        if (envOverride)
        {
            ApplyEnvironmentOverrides(connectionDict, envOverrides);
        }

        // If a name is provided, override Initial Catalog in the connection dictionary
        if (!string.IsNullOrWhiteSpace(name))
        {
            connectionDict["Initial Catalog"] = name;
        }

        connectionTags = [.. connectionDict.Select(kvp => $"{kvp.Key}={kvp.Value}")];

        foreach (string? value in connectionTags)
        {
            Debug.WriteLine($"\t\tConfig: {value}");
        }

        string connectionString = string.Join(';', connectionTags);

        Debug.WriteLine($"\t\tConfig: {connectionString}");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception($"Connection string is null or empty for environment: {environment}");
        }

        SqlConnectionStringBuilder builder = new(connectionString)
        {
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
    #endregion

    #region helpers
    /// <summary>
    /// Builds the configuration root for the specified environment.
    /// </summary>
    private static IConfiguration? BuildConfiguration(string environment)
    {
        ConfigurationBuilder builder = new();

        string? portfolioDir = FindPortfolioProjectDirectory();
        string? absBase = EnsureAbsolutePath(portfolioDir!);

        // Only set base path if it exists, otherwise fallback to current directory
        if (!string.IsNullOrWhiteSpace(absBase) && Directory.Exists(absBase))
        {
            _ = builder.SetBasePath(absBase);
        }
        else
        {
            // fallback: use current directory (works in Docker/test environments)
            _ = builder.SetBasePath(Directory.GetCurrentDirectory());
        }
        AddJsonFiles(builder, environment);
        return builder.Build();
    }

    /// <summary>
    /// Adds JSON configuration files for the given environment.
    /// </summary>
    private static void AddJsonFiles(ConfigurationBuilder builder, string environment)
    {
        _ = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            _ = builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
            //if (string.Equals(environment, "Release", StringComparison.OrdinalIgnoreCase))
            //{
            //    _ = builder.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: false);
            //}
        }
        _ = builder.AddJsonFile("secrets.json", optional: true, reloadOnChange: false);
    }

    /// <summary>
    /// Ensures the given path is absolute.
    /// </summary>
    private static string? EnsureAbsolutePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : Path.IsPathRooted(path) ? Path.GetFullPath(path) : TryGetFullPath(path);
    }

    /// <summary>
    /// Attempts to get the full path for the given path.
    /// </summary>
    private static string? TryGetFullPath(string path)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            string combined = Path.Combine(baseDir, path);
            return Path.GetFullPath(combined);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the portfolio project directory based on the assembly location.
    /// </summary>
    private static string? FindPortfolioProjectDirectory()
    {
        string? baseDir = AppContext.BaseDirectory;
        if (!IsValidDirectory(baseDir)) return string.Empty;

        string? assemblyDirectory = GetAssemblyDirectory();
        if (!IsValidDirectory(assemblyDirectory)) return string.Empty;

        Debug.WriteLine($"[DEBUG] Original assemblyDirectory: {assemblyDirectory}");
        string testReplacement = ReplaceTestDirectory(assemblyDirectory!);
        Debug.WriteLine($"[DEBUG] Would replace with: {testReplacement}");

        string? assemblyName = GetAssemblyName();
        if (string.IsNullOrWhiteSpace(assemblyName)) return string.Empty;

        string[] parts = GetPathParts(assemblyDirectory!);
        string[] assambplePath = RemoveBeforeAssembly(parts, assemblyName);
        string[] baseLocation = RemoveAfterAssembly(parts, assemblyName);

        LogDebugPaths(baseLocation, assambplePath);

        string baseCombined = Path.Combine(baseLocation);
        string prefix = NeedsPathPrefix(assemblyDirectory!, baseCombined) ? Path.DirectorySeparatorChar.ToString() : string.Empty;
        string finalPath = CombinePortfolioPath(prefix, baseCombined, assambplePath);
        Debug.WriteLine($"[DEBUG] Final portfolio path: {finalPath}");

        return finalPath;
    }

    private static bool IsValidDirectory(string? dir)
    {
        return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);
    }

    private static string ReplaceTestDirectory(string assemblyDirectory)
    {
        return assemblyDirectory.Contains("Portfolio.Infrastructure.Tests")
            ? assemblyDirectory.Replace("Portfolio.Infrastructure.Tests", "Portfolio")
            : assemblyDirectory;
    }

    private static string? GetAssemblyName()
    {
        return typeof(DbContextOptionsServices).Assembly.GetName().Name;
    }

    private static string[] RemoveBeforeAssembly(string[] parts, string assemblyName)
    {
        return parts.RemoveBefore(p => !string.IsNullOrEmpty(p) && p.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase), false);
    }

    private static string[] RemoveAfterAssembly(string[] parts, string assemblyName)
    {
        return parts.RemoveAfter(p => !string.IsNullOrEmpty(p) && p.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase), false);
    }

    /// <summary>
    /// Replaces configuration tokens in a value with their resolved values.
    /// </summary>
    private string? ReplaceTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || _config == null)
            return value;

        // If the value is a token (e.g., {SomeKey}), resolve it from configuration
        if (value.StartsWith('{') && value.EndsWith('}'))
        {
            string token = value[1..^1];
            string? resolvedValue = _config.GetValue<string>(token);
            DebugInfo($"Resolved token '{token}' to '{resolvedValue}'", nameof(DbContextOptionsServices));
            return resolvedValue;
        }

        // Not a token placeholder — return the original value.
        return value;
    }

    /// <summary>
    /// Logs debug information about the paths used.
    /// </summary>
    [Conditional("DEBUG")]
    private static void LogDebugPaths(string[] baseLocation, string[] assambplePath)
    {
        Debug.WriteLine($"\n\nBaseLocation: {string.Join(Path.DirectorySeparatorChar, baseLocation)}");
        Debug.WriteLine($"AssambplePath: {string.Join(Path.DirectorySeparatorChar, assambplePath)}\n\n");
    }

    /// <summary>
    /// Logs debug information with optional class and member name.
    /// </summary>
    [Conditional("DEBUG")]
    private static void DebugInfo(string message, string? className = null, [CallerMemberName] string memberName = "")
    {
        if (className is not null)
        {
            Debug.WriteLine($"{className}.{memberName}\n\t{message}");
            return;
        }
        Debug.WriteLine($"{memberName} - {message}");
    }
    #endregion

    #region environment variable helpers
    /// <summary>
    /// Gets connection string overrides from environment variables.
    /// </summary>
    private static ConcurrentDictionary<string, string> GetEnvironmentConnectionOverrides()
    {
        (string envVar, string connKey)[] envMap =
        [
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__DATASOURCE", "Data Source"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__INITIALCATALOG", "Initial Catalog"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__USERID", "User ID"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__PASSWORD", "Password"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__MULTIPLEACTIVERESULTSETS", "MultipleActiveResultSets"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__TRUSTSERVER", "TrustServerCertificate"),
            ("DOTNET_PORTFOLIO_CONNECTIONSTRINGS__DEFAULTCONNECTION__ENCRYPT", "Encrypt"),
        ];
        ConcurrentDictionary<string, string> overrides = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string? envVar, string? connKey) in envMap)
        {
            string? val = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(val))
            {
                overrides[connKey] = val;
            }
        }
        return overrides;
    }

    /// <summary>
    /// Applies environment variable overrides to the connection dictionary.
    /// </summary>
    private static void ApplyEnvironmentOverrides(ConcurrentDictionary<string, string> connectionDict, ConcurrentDictionary<string, string> envOverrides)
    {
        foreach (KeyValuePair<string, string> kvp in envOverrides)
        {
            connectionDict[kvp.Key] = kvp.Value;
        }
    }
    #endregion

    #region config extraction helpers
    /// <summary>
    /// Builds a dictionary of connection string key-value pairs from the configuration section.
    /// </summary>
    private ConcurrentDictionary<string, string> BuildConnectionDictionaryFromConfig(IConfigurationSection configSection)
    {
        ConcurrentDictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string?> e in configSection.AsEnumerable())
        {
            Debug.WriteLine($"\t\tConnection Config: {e.Key}={e.Value}");
            string? resolvedValue = ReplaceTokens(e.Value);
            if (resolvedValue != null)
            {
                string key = e.Key.Split(':').Last();
                dict[key] = resolvedValue;
                _ = (_config?[e.Key] = resolvedValue);
            }
        }
        return dict;
    }
    #endregion

    #region Path helpers
    /// <summary>
    /// Gets the directory of the executing assembly.
    /// </summary>
    private static string? GetAssemblyDirectory()
    {
        string? location = Assembly.GetExecutingAssembly().Location;
        return string.IsNullOrEmpty(location) ? null : Path.GetDirectoryName(location);
    }

    /// <summary>
    /// Splits a path into its parts.
    /// </summary>
    private static string[] GetPathParts(string path)
    {
        return string.IsNullOrEmpty(path)
            ? []
            : path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Determines if a path prefix is needed.
    /// </summary>
    private static bool NeedsPathPrefix(string assemblyDirectory, string baseCombined)
    {
        return !string.IsNullOrEmpty(assemblyDirectory)
            && (assemblyDirectory[0] == Path.DirectorySeparatorChar || assemblyDirectory[0] == Path.AltDirectorySeparatorChar)
            && (string.IsNullOrEmpty(baseCombined) || (baseCombined[0] != Path.DirectorySeparatorChar && baseCombined[0] != Path.AltDirectorySeparatorChar));
    }

    /// <summary>
    /// Combines the portfolio path from its parts.
    /// </summary>
    private static string CombinePortfolioPath(string prefix, string baseCombined, string[] assambplePath)
    {
        return prefix + baseCombined
            + Path.DirectorySeparatorChar
            + "Portfolio"
            + Path.DirectorySeparatorChar
            + Path.Combine(assambplePath);
    }
    #endregion
}
