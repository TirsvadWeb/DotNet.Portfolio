using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Portfolio.Components;
using Portfolio.Components.Account;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Persistents;
using Portfolio.Middleware;

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
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

        // register certificate service and infrastructure (EF Core)
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies();


        // Register cookie authentication so we can establish an authenticated session from a client certificate
        //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        //    .AddCookie(options =>
        //    {
        //        options.Cookie.Name = "Portfolio.Auth";
        //        options.LoginPath = "/login";
        //        options.Cookie.SameSite = SameSiteMode.Lax;
        //    });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // Only enable HTTPS redirection if an HTTPS endpoint is configured.
        // This avoids the runtime warning when the container is serving HTTP only (common in Docker setups)
        // Moved to extension method to keep Program.cs minimal
        app.UseConditionalHttpsRedirection(builder.Configuration);

        app.UseAntiforgery();

        app.MapStaticAssets();



        // Apply any pending migrations at startup
        using (IServiceScope scope = app.Services.CreateScope())
        {
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            //db.Database.Migrate();
        }

        // Middleware: attempt to autoload a predefined X509 certificate (if present) and authenticate the user
        app.UseMiddleware<PreloadedX509CertificateMiddleware>();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}
