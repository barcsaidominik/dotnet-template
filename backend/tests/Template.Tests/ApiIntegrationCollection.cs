namespace Template.Tests;

[CollectionDefinition("Api integration", DisableParallelization = true)]
public sealed class ApiIntegrationCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
}
