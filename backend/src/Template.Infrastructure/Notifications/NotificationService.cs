using System.Globalization;
using Template.Application.Common.Notifications;
using Template.Infrastructure.Notifications.Channels;

namespace Template.Infrastructure.Notifications;

public sealed class NotificationService(IEnumerable<INotificationChannel> channels) : INotificationService
{
    private readonly INotificationChannel[] _channels = channels.ToArray();

    public async Task SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        foreach (var channelType in request.Channels.Distinct())
        {
            var channel = _channels.FirstOrDefault(x => x.CanHandle(channelType));
            if (channel is null)
            {
                throw new NotSupportedException($"A(z) '{channelType}' értesítési csatorna nincs regisztrálva.");
            }

            await SendWithCultureAsync(channel, request, ct);
        }
    }

    private static async Task SendWithCultureAsync(INotificationChannel channel, NotificationRequest request, CancellationToken ct)
    {
        if (request.Culture is null)
        {
            await channel.SendAsync(request, ct);
            return;
        }

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = request.Culture;
            CultureInfo.CurrentUICulture = request.Culture;

            await channel.SendAsync(request, ct);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
