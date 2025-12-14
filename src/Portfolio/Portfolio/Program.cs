using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

using Portfolio.Components;
using Portfolio.Components.Account;
using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Persistents;

using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
        app.UseHttpsRedirection();

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
                X509Certificate2? cert = certService?.GetPreloadedCertificate();
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

                            List<Claim> claims = new()
                            {
                                new Claim(ClaimTypes.Name, cert.Subject ?? string.Empty),
                                new Claim("thumbprint", cert.Thumbprint ?? string.Empty),
                                new Claim("issuer", cert.Issuer ?? string.Empty),
                                new Claim("serialNumber", cert.SerialNumber ?? string.Empty)
                            };

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
