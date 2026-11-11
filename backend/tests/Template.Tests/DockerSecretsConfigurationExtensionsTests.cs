using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Template.Common.Configuration;

namespace Template.Tests;

public class DockerSecretsConfigurationExtensionsTests : IDisposable
{
    private readonly string _secretsPath = Path.Combine(Path.GetTempPath(), $"docker-secrets-{Guid.NewGuid()}");

    public DockerSecretsConfigurationExtensionsTests()
    {
        Directory.CreateDirectory(_secretsPath);
    }

    public void Dispose()
    {
        Directory.Delete(_secretsPath, recursive: true);
    }

    [Fact]
    public void AddDockerSecrets_MapsSecretFiles_ToConfigurationKeys_AndTrimsThem()
    {
        File.WriteAllText(Path.Combine(_secretsPath, "jwt_secret"), "jwt-value\n");
        File.WriteAllText(Path.Combine(_secretsPath, "internal_service_token"), "  token-value  ");

        var configuration = new ConfigurationBuilder()
            .AddDockerSecrets(_secretsPath)
            .Build();

        configuration["JwtSettings:Secret"].Should().Be("jwt-value");
        configuration["InternalServiceAuth:Token"].Should().Be("token-value");
    }

    [Fact]
    public void AddDockerSecrets_OverridesEarlierSources()
    {
        File.WriteAllText(Path.Combine(_secretsPath, "jwt_secret"), "from-secret");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:Secret"] = "from-env" })
            .AddDockerSecrets(_secretsPath)
            .Build();

        configuration["JwtSettings:Secret"].Should().Be("from-secret");
    }

    [Fact]
    public void AddDockerSecrets_IgnoresUnmappedFiles_AndMissingDirectory()
    {
        File.WriteAllText(Path.Combine(_secretsPath, "db_password"), "not-mapped");

        var configuration = new ConfigurationBuilder()
            .AddDockerSecrets(_secretsPath)
            .AddDockerSecrets(Path.Combine(_secretsPath, "does-not-exist"))
            .Build();

        configuration.AsEnumerable().Should().BeEmpty();
    }
}
