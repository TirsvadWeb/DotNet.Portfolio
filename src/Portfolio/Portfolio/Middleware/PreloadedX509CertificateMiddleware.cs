using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;

using Portfolio.Core.Abstracts;
using Portfolio.Core.Abstracts.Services;

using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Middleware;

/// <summary>
/// Middleware that looks for a predefined X509 certificate in the current user's "My" store
/// and, when found, adds it to the request <see cref="HttpContext.Items"/> and optionally
/// signs in a user via cookie authentication.
/// </summary>
/// <remarks>
/// This middleware is intended to ease development and automated scenarios where a known
/// certificate is available on the machine and should be used to establish an authenticated
/// user context automatically.
/// 
/// Inheriting XML documentation with <c>&lt;inheritdoc/&gt;</c>:
/// - Use <c>&lt;inheritdoc/&gt;</c> on implementation members to inherit documentation from an
///   interface or base class. This keeps documentation DRY and ensures the contract is
///   documented in one place.
/// - Example: an interface method can contain the canonical summary and an implementing
///   class can put <c>&lt;inheritdoc/&gt;</c> on its method to reuse that summary and any
///   remarks.
/// 
/// Why we have it:
/// - Reduces duplication across implementations.
/// - Keeps documentation consistent when the contract changes.
/// - Helps maintainers quickly find the authoritative documentation.
/// 
/// Example usage (registering middleware in Program.cs):
/// <code>
/// // in Program.cs
/// app.UseMiddleware&lt;PreloadedX509CertificateMiddleware&gt;();
/// 
/// // Example of inheriting documentation from an interface:
/// public interface IGreeter
/// {
///     /// &lt;summary&gt;Return a greeting for the specified name.&lt;/summary&gt;
///     string Greet(string name);
/// }
/// 
/// /// &lt;summary&gt;A greeter implementation that reuses the interface docs.&lt;/summary&gt;
/// /// &lt;inheritdoc/&gt;
/// public class FriendlyGreeter : IGreeter
/// {
///     public string Greet(string name) => $"Hello, {name}!";
/// }
/// </code>
/// </remarks>
public sealed class PreloadedX509CertificateMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    /// <summary>
    /// Invokes the middleware which will check for a preloaded certificate and optionally
    /// sign in the user using the configured <see cref="ICertificateSignInService"/>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when the middleware has finished processing the request.</returns>
    /// <remarks>
    /// This method will add the certificate (if found) into <see cref="HttpContext.Items"/>
    /// using the key "PreloadedX509Certificate". It attempts to sign the user in only when
    /// the current context is not already authenticated.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        Debug.WriteLine("Checking for predefined X509 certificate...");
        try
        {
            IX509CertificateService? certService = context.RequestServices.GetService<IX509CertificateService>();
            ICertificateSignInService? signInService = context.RequestServices.GetService<ICertificateSignInService>();

            IConfiguration? configuration = context.RequestServices.GetService<IConfiguration>();
            string certNamespace = configuration?.GetValue<string>("ClientCertificateAuth:Namespace") ?? "TirsvadWebCert";

            X509Certificate2? cert = null;
            if (certService != null)
            {
                cert = await certService.GetPreloadedCertificateAsync(certNamespace);
            }

            Debug.WriteLine("Preloaded certificate: {0}", cert != null ? cert.Subject : "null");

            if (cert != null)
            {
                // Store the preloaded certificate on the current request so other middleware/components can use it.
                context.Items["PreloadedX509Certificate"] = cert;
                Debug.WriteLine("Predefined certificate found in CurrentUser\\My store.");

                if (!(context.User?.Identity?.IsAuthenticated ?? false))
                {
                    try
                    {
                        if (signInService != null)
                        {
                            ClaimsPrincipal? principal = await signInService.CreatePrincipalForCertificateAsync(cert);
                            if (principal != null)
                            {
                                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });
                                Debug.WriteLine("ApplicationUser signed in automatically via preloaded X509 certificate.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("ICertificateSignInService not registered; skipping sign-in.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to sign in with certificate: {0}", ex);
                        Debug.Fail("Failed to sign in with certificate.\n" + ex);
                    }
                }
            }
            else
            {
                Debug.WriteLine("Predefined certificate not found in CurrentUser\\My store.");
            }
        }
        catch
        {
            // ignore any errors while attempting to read certificate
        }

        await _next(context);
    }
}
