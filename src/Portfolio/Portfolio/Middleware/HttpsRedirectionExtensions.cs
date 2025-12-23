using System.Diagnostics;

namespace Portfolio.Middleware;

/// <summary>
/// Provides extension methods to configure HTTP -> HTTPS redirection conditionally.
/// </summary>
/// <remarks>
/// This class contains helpers used by the application's startup to enable HTTPS
/// redirection only when an HTTPS endpoint is actually configured. This avoids the
/// runtime warning that occurs when running inside containers or environments that
/// expose only HTTP.
/// 
/// Inheriting XML documentation with <c>&lt;inheritdoc/&gt;</c>:
/// - Use <c>&lt;inheritdoc/&gt;</c> on members that override or implement other members
///   to reuse their documentation. This keeps docs consistent and reduces duplication.
/// - You can also use <c>&lt;inheritdoc cref="Fully.Qualified.MemberName"/&gt;</c>
///   to inherit documentation from another symbol explicitly.
/// 
/// Example (how to inherit docs from a base method):
/// <code>
/// // Base type
/// public class BaseService
/// {
///     /// <summary>Performs work.</summary>
///     public virtual void DoWork() { }
/// }
/// 
/// // Derived type inherits the documentation of BaseService.DoWork
/// public class DerivedService : BaseService
/// {
///     /// <inheritdoc cref="BaseService.DoWork" />
///     public override void DoWork() { /* implementation */ }
/// }
/// </code>
/// 
/// Example (how to use this extension in Program.cs):
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// var app = builder.Build();
///
/// // Ensures we only call UseHttpsRedirection when an HTTPS endpoint is actually available
/// app.UseConditionalHttpsRedirection(builder.Configuration);
///
/// app.Run();
/// </code>
/// </remarks>
public static class HttpsRedirectionExtensions
{
    /// <summary>
    /// Adds HTTPS redirection middleware only when an HTTPS endpoint appears to be
    /// configured via configuration or environment variables.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <param name="configuration">Application configuration (used to inspect settings like <c>ASPNETCORE_HTTPS_PORT</c>).</param>
    /// <example>
    /// <code>
    /// // Call from Program.cs
    /// app.UseConditionalHttpsRedirection(builder.Configuration);
    /// </code>
    /// </example>
    public static void UseConditionalHttpsRedirection(this WebApplication app, IConfiguration configuration)
    {
        // Determine whether an HTTPS endpoint is configured (environment or configuration).
        // If not, skip UseHttpsRedirection to avoid runtime redirect warnings in HTTP-only hosts (e.g. some Docker setups).
        bool httpsConfigured = false;
        string? httpsPortEnv = configuration["ASPNETCORE_HTTPS_PORT"];
        string? aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? configuration["ASPNETCORE_URLS"];
        if (!string.IsNullOrWhiteSpace(httpsPortEnv))
        {
            httpsConfigured = true;
        }
        else if (!string.IsNullOrWhiteSpace(aspnetcoreUrls) && aspnetcoreUrls.Contains("https", StringComparison.OrdinalIgnoreCase))
        {
            httpsConfigured = true;
        }

        if (httpsConfigured)
        {
            app.UseHttpsRedirection();
        }
        else
        {
            Debug.WriteLine("INFO: HTTPS endpoint not detected; skipping UseHttpsRedirection to avoid redirect warnings.");
        }
    }
}
