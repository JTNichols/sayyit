using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Sayyit.Web.Services;

public sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException("Configuration value 'Api:BaseUrl' is required.");
        }

        var apiScope = configuration["Api:Scope"];
        if (string.IsNullOrWhiteSpace(apiScope))
        {
            throw new InvalidOperationException("Configuration value 'Api:Scope' is required.");
        }

        ConfigureHandler(
            authorizedUrls: [apiBaseUrl],
            scopes: [apiScope]);
    }
}
