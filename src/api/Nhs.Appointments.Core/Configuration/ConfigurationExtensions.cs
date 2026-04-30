using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Nhs.Appointments.Core.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddMyaConfiguration(this IConfigurationBuilder builder)
    {
        var tempConfig = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var vaultUri = tempConfig["KEY_VAULT_URI"];

        builder.AddEnvironmentVariables();

        if (!string.IsNullOrEmpty(vaultUri))
        {
            builder.AddAzureKeyVault(
                new Uri(vaultUri),
                new DefaultAzureCredential(),
                new NhsKeyVaultManager()); 
        }

        return builder;
    }

    private class NhsKeyVaultManager : KeyVaultSecretManager
    {
        public override string GetKey(KeyVaultSecret secret)
        {
            return secret.Name.Replace("-", "_");
        }
    }
}
