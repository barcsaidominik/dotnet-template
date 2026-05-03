using Microsoft.Extensions.Configuration;

namespace Template.Common.Configuration;

public static class DockerSecretsConfigurationExtensions
{
    private static readonly Dictionary<string, string> _secretMappings = new()
    {
        ["jwt_secret"] = "JwtSettings:Secret",
        ["internal_service_token"] = "InternalServiceAuth:Token",
        ["db_password"] = "Database:Password"
    };

    public static IConfigurationBuilder AddDockerSecrets(
        this IConfigurationBuilder builder,
        string secretsPath = "/run/secrets")
    {
        secretsPath = Path.GetFullPath(secretsPath);

        if (!Directory.Exists(secretsPath))
        {
            return builder;
        }

        var secrets = new Dictionary<string, string?>();

        foreach (var (fileName, configKey) in _secretMappings)
        {
            var filePath = Path.Combine(secretsPath, fileName);
            if (File.Exists(filePath))
            {
                secrets[configKey] = File.ReadAllText(filePath).Trim();
            }
        }

        if (secrets.Count > 0)
        {
            builder.AddInMemoryCollection(secrets);
        }

        return builder;
    }
}
