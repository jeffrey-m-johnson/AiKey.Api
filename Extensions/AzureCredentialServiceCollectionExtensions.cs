using Azure.Core;
using Azure.Identity;

namespace AiKey.Api.Extensions;

public static class AzureCredentialServiceCollectionExtensions
{
    public static IServiceCollection AddAzureCredential(this IServiceCollection services, IConfiguration configuration)
    {


        services.AddKeyedTransient<TokenCredential>(Constants.NamedServices.AzureAiServiceCredential, (serviceProvider, key) =>
        {
            string? tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            string? clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string? clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");


            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException("TenantId, ClientId, and ClientSecret must be provided.");
            }

            TokenCredential credential = new ClientSecretCredential(
                tenantId: tenantId,
                clientId: clientId,
                clientSecret: clientSecret);

            return credential;
        });

        return services;
    }
}
