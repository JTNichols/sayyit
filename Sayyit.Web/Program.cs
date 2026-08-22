using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Sayyit.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            var environment = builder.HostEnvironment.Environment;
            var configuredAuthority = builder.Configuration["AzureAd:Authority"];
            var configuredClientId = builder.Configuration["AzureAd:ClientId"];
            Console.WriteLine($"[AuthConfig] Environment: {environment}; Authority: {configuredAuthority}; ClientIdConfigured: {!string.IsNullOrWhiteSpace(configuredClientId)}");

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

                options.ProviderOptions.LoginMode = "redirect";
                options.ProviderOptions.Authentication.KnownAuthorities.Add("sayyit.ciamlogin.com");

                options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
                options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
                 
                //builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
                //options.ProviderOptions.DefaultAccessTokenScopes
                //    .Add("https://graph.microsoft.com/User.Read");
                //options.ProviderOptions.DefaultAccessTokenScopes.Add("{TODO: SCOPE URI}");
                //options.ProviderOptions.AdditionalScopesToConsent.Add("{ADDITIONAL SCOPE URI}");
            });

            await builder.Build().RunAsync();
        }
    }
}
