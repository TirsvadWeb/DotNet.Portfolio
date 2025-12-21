using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

using Portfolio.Components;
using Portfolio.Components.Account;
using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Persistents;

using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Portfolio;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configure data protection key persistence so keys survive process/container restarts.
        // Path can be overridden by configuration: "DataProtection:KeyPath" (useful for Docker volumes).
        string? dpPath = builder.Configuration["DataProtection:KeyPath"] ?? builder.Configuration["DataProtection__KeyPath"];
        if (string.IsNullOrWhiteSpace(dpPath))
        {
            dpPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
        }

        try
        {
            Directory.CreateDirectory(dpPath!);
            builder.Services.AddDataProtection()
                .SetApplicationName("Portfolio")
                .PersistKeysToFileSystem(new DirectoryInfo(dpPath!));
        }
        catch (Exception ex)
        {
            // Surface an explicit error so Docker / logs show why keys aren't persisted.
            Console.Error.WriteLine($"WARNING: Failed to persist data-protection keys to '{dpPath}'. Falling back to in-memory key ring. Ensure the path is mounted and writable. Exception: {ex.Message}");
        }

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

        // register certificate service and infrastructure (EF Core)
        builder.Services.AddInfrastructureServices(builder.Configuration);

        // Register cookie authentication so we can establish an authenticated session from a client certificate
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Portfolio.Auth";
                options.LoginPath = "/login";
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // Only enable HTTPS redirection if an HTTPS endpoint is configured.
        // This avoids the runtime warning when the container is serving HTTP only (common in Docker setups).
        bool httpsConfigured = false;
        string? httpsPortEnv = builder.Configuration["ASPNETCORE_HTTPS_PORT"];
        string? aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? builder.Configuration["ASPNETCORE_URLS"];
        if (!string.IsNullOrWhiteSpace(httpsPortEnv))
        {
            httpsConfigured = true;
        }
        else if (!string.IsNullOrWhiteSpace(aspnetcoreUrls) && aspnetcoreUrls.IndexOf("https", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            httpsConfigured = true;
        }

        if (httpsConfigured)
        {
            app.UseHttpsRedirection();
        }
        else
        {
            Console.WriteLine("INFO: HTTPS endpoint not detected; skipping UseHttpsRedirection to avoid redirect warnings.");
        }

        app.UseAntiforgery();

        // Apply any pending migrations at startup
        using (IServiceScope scope = app.Services.CreateScope())
        {
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        }


        // Enable authentication/authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware: attempt to autoload a predefined X509 certificate (if present)
        app.Use(async (context, next) =>
        {
            try
            {
                IX509CertificateService? certService = context.RequestServices.GetService<IX509CertificateService>();
                X509Certificate2? cert = certService?.GetPreloadedCertificateAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (cert != null)
                {
                    context.Items["PreloadedX509Certificate"] = cert;

                    // If a certificate is found and the user is not already authenticated,
                    // create a ClaimsPrincipal and sign in using cookie authentication.
                    if (!(context.User?.Identity?.IsAuthenticated ?? false))
                    {
                        try
                        {
                            IClientCertificateRepository? repo = context.RequestServices.GetService<IClientCertificateRepository>();
                            if (repo != null)
                            {
                                // Ensure certificate is recorded in database (idempotent add)
                                ClientCertificate? existing = await repo.FindBySubjectAsync(cert.Subject ?? string.Empty);
                                if (existing == null)
                                {
                                    ClientCertificate entity = new()
                                    {
                                        Id = Guid.NewGuid(),
                                        Subject = cert.Subject ?? string.Empty,
                                        Issuer = cert.Issuer ?? string.Empty,
                                        SerialNumber = cert.SerialNumber ?? string.Empty,
                                        ValidFrom = cert.NotBefore,
                                        ValidTo = cert.NotAfter
                                    };

                                    await repo.AddAsync(entity);
                                }
                            }

                            List<Claim> claims =
                            [
                                new Claim(ClaimTypes.Name, cert.Subject ?? string.Empty),
                                new Claim("thumbprint", cert.Thumbprint ?? string.Empty),
                                new Claim("issuer", cert.Issuer ?? string.Empty),
                                new Claim("serialNumber", cert.SerialNumber ?? string.Empty)
                            ];

                            ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            ClaimsPrincipal principal = new(identity);

                            // Sign in (non-persistent by default)
                            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });

                            Debug.WriteLine("User signed in automatically via preloaded X509 certificate.");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to sign in with certificate: {ex.Message}");
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

            await next();
        });

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}
