using System.Text.Json;
using Template.Domain.Common;

namespace Template.Domain.Entities;

public sealed class MailboxMessage : Entity
{
    public Guid RecipientUserId
    {
        get; private set;
    }
    public string Category
    {
        get; private set;
    } = string.Empty;
    public string TitleKey
    {
        get; private set;
    } = string.Empty;
    public string BodyKey
    {
        get; private set;
    } = string.Empty;
    public string? ParametersJson
    {
        get; private set;
    }
    public string? Link
    {
        get; private set;
    }
    public bool IsRead
    {
        get; private set;
    }
    public DateTime? ReadAtUtc
    {
        get; private set;
    }

    private MailboxMessage()
    {
    }

    public static MailboxMessage Create(
        Guid recipientUserId,
        string category,
        string titleKey,
        string bodyKey,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? link = null)
    {
        return new MailboxMessage
        {
            RecipientUserId = recipientUserId,
            Category = category,
            TitleKey = titleKey,
            BodyKey = bodyKey,
            ParametersJson = SerializeParameters(parameters),
            Link = link,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyDictionary<string, string> GetParameters()
    {
        if (string.IsNullOrWhiteSpace(ParametersJson))
        {
            Dictionary<string, string> emptyParameters = [];
            return emptyParameters;
        }

        var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(ParametersJson);
        if (parameters is not null)
        {
            return parameters;
        }

        Dictionary<string, string> fallbackParameters = [];
        return fallbackParameters;
    }

    private static string? SerializeParameters(IReadOnlyDictionary<string, string>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(parameters);
    }
}
