using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sayyit.Web.Services;

namespace Sayyit.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            //var environment = builder.HostEnvironment.Environment;
            //var currentHost = new Uri(builder.HostEnvironment.BaseAddress).Host;

            //var environmentSettingsFile = currentHost.Contains("sayyit-dev-webapp.azurewebsites.net", StringComparison.OrdinalIgnoreCase)
            //    ? "appsettings.Dev.json"
            //    : $"appsettings.{environment}.json";

            //using (var startupHttpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
            //{
            //    using var environmentSettingsResponse = await startupHttpClient.GetAsync(environmentSettingsFile);
            //    if (environmentSettingsResponse.IsSuccessStatusCode)
            //    {
            //        await using var environmentSettingsStream = await environmentSettingsResponse.Content.ReadAsStreamAsync();
            //        builder.Configuration.AddJsonStream(environmentSettingsStream);
            //    }
            //    else
            //    {
            //        Console.WriteLine($"[AuthConfig] Environment settings file '{environmentSettingsFile}' was not found. Status: {(int)environmentSettingsResponse.StatusCode}");
            //    }
            //}

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<ApiAuthorizationMessageHandler>();

            var configuredAuthority = builder.Configuration["AzureAd:Authority"];
            var configuredClientId = builder.Configuration["AzureAd:ClientId"];
            Console.WriteLine($"[AuthConfig] Environment: {environment}; Authority: {configuredAuthority}; ClientIdConfigured: {!string.IsNullOrWhiteSpace(configuredClientId)}");

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

                options.ProviderOptions.LoginMode = "redirect";
                options.ProviderOptions.Authentication.KnownAuthorities.Add("sayyit.ciamlogin.com");
                options.ProviderOptions.Authentication.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
                options.ProviderOptions.Authentication.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;

                options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
                options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");

                var configuredApiScope = builder.Configuration["Api:Scope"];
                if (!string.IsNullOrWhiteSpace(configuredApiScope))
                {
                    options.ProviderOptions.DefaultAccessTokenScopes.Add(configuredApiScope);
                }
            });

            var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException("Configuration value 'Api:BaseUrl' is required.");
            }

            var apiBaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);

            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = apiBaseAddress;
            })
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

            Console.WriteLine($"Client Hosting Environment: {builder.HostEnvironment.Environment}");
            Console.WriteLine($"Sayyit configuration ClientId: {builder.Configuration["AzureAd:ClientId"]}");
            Console.WriteLine($"Sayyit API scope: {builder.Configuration["Api:Scope"]}");
            await builder.Build().RunAsync();
        }
    }
}
