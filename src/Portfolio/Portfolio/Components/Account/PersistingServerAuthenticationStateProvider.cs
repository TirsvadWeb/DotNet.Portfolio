using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Portfolio.Domain.Entities;

using System.Security.Claims;

namespace Portfolio.Components.Account;

internal sealed class PersistingServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly PersistentComponentState? _state;
    private readonly IdentityOptions? _options;
    private readonly PersistingComponentStateSubscription? _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState persistentComponentState,
        IOptions<IdentityOptions> optionsAccessor
        )
    {
        _state = persistentComponentState;
        _options = optionsAccessor.Value;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _authenticationStateTask = task;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask == null)
        {
            throw new InvalidOperationException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
        }

        if (_authenticationStateTask is not null)
        {
            AuthenticationState authenticationState = await _authenticationStateTask.ConfigureAwait(false);
            ClaimsPrincipal principal = authenticationState.User;
            if (principal.Identity?.IsAuthenticated == true)
            {
                string? userId = principal.FindFirst(_options!.ClaimsIdentity.UserIdClaimType)?.Value;
                string? email = principal.FindFirst(_options.ClaimsIdentity.EmailClaimType)?.Value;

                if (userId is not null && email is not null)
                {
                    _state!.PersistAsJson(nameof(ApplicationUser), new ApplicationUser
                    {
                        Id = Guid.Parse(userId),
                        Email = email,
                    });
                }
            }
        }
    }

    public void Dispose()
    {
        // Dispose resources if needed
    }
}
