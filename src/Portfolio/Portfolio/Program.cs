using Portfolio.Components;
using Portfolio.Core;
using Portfolio.Infrastructure;

using System.Diagnostics;
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

        // register certificate service
        builder.Services.AddInfrastructureServices();

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
