using System.Globalization;

namespace Template.Infrastructure.Templating;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateGroup, string templateName, string templatePart, object model, CultureInfo? culture = null, CancellationToken ct = default);
}
