namespace Template.Tests;

[CollectionDefinition("Products integration", DisableParallelization = true)]
public sealed class ProductsIntegrationCollection : ICollectionFixture<ProductsIntegrationTestWebApplicationFactory>
{
}
