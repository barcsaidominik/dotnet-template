using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Template.Application.Common.Dtos;

namespace Template.Tests;

[Collection("Api integration")]
public class MailboxIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ReadAllFlow_WithAuthenticatedFacilityAdmin_ResetsUnreadCount()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var unreadBeforeResponse = await client.GetAsync("/api/mailbox/unread-count", ct);
        var unreadBefore = await unreadBeforeResponse.Content.ReadFromJsonAsync<MailboxUnreadCountDto>(cancellationToken: ct);

        var readAllResponse = await client.PostAsync("/api/mailbox/read-all", null, ct);

        var unreadAfterResponse = await client.GetAsync("/api/mailbox/unread-count", ct);
        var unreadAfter = await unreadAfterResponse.Content.ReadFromJsonAsync<MailboxUnreadCountDto>(cancellationToken: ct);

        // Assert
        unreadBeforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        unreadBefore.Should().NotBeNull();
        unreadBefore!.UnreadCount.Should().Be(2);

        readAllResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        unreadAfterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        unreadAfter.Should().NotBeNull();
        unreadAfter!.UnreadCount.Should().Be(0);
    }
}
