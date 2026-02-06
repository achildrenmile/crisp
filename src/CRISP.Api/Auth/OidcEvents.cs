using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CRISP.Api.Auth;

/// <summary>
/// Custom OIDC events for handling AJAX requests and authentication flows.
/// </summary>
public static class OidcEvents
{
    /// <summary>
    /// Creates OpenIdConnect events that handle AJAX requests appropriately.
    /// </summary>
    public static OpenIdConnectEvents CreateOidcEvents()
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                // For AJAX requests, return 401 instead of redirect
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.Append("X-Auth-Required", "OIDC");
                    context.HandleResponse();
                }
                return Task.CompletedTask;
            },
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                // For AJAX requests, just clear the session
                if (IsAjaxRequest(context.Request))
                {
                    context.HandleResponse();
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // Log authentication failures
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("OIDC authentication failed: {Error}",
                    context.Exception?.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Optional: Add custom claims or perform additional validation
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("OIDC token validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Creates cookie authentication events that handle AJAX requests appropriately.
    /// </summary>
    public static CookieAuthenticationEvents CreateCookieEvents()
    {
        return new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // For AJAX requests, return 401 instead of redirect
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.Append("X-Auth-Required", "Cookie");
                    context.Response.Headers.Append("X-Login-Url", context.RedirectUri);
                    return Task.CompletedTask;
                }

                // For browser requests, redirect to login
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                // For AJAX requests, return 403 instead of redirect
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToLogout = context =>
            {
                // For AJAX requests, just clear the cookie
                if (IsAjaxRequest(context.Request))
                {
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Determines if the request is an AJAX/API request.
    /// </summary>
    private static bool IsAjaxRequest(HttpRequest request)
    {
        // Check X-Requested-With header (jQuery style)
        if (request.Headers.XRequestedWith == "XMLHttpRequest")
            return true;

        // Check Accept header for JSON
        var accept = request.Headers.Accept.ToString();
        if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check Content-Type header
        var contentType = request.ContentType;
        if (!string.IsNullOrEmpty(contentType) &&
            contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if it's an API path
        if (request.Path.StartsWithSegments("/api"))
            return true;

        return false;
    }
}
